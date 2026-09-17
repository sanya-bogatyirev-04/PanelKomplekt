using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace PanelKomplekt.Core
{
    /// <summary>
    /// Фоновое обновление марок панелей. Работает всегда, пока загружен плагин, кнопки нет.
    /// Во время команды запоминает изменённые и добавленные вставки блоков (растягивание ручкой, РАСТЯНУТЬ,
    /// «Свойства», копирование, массив, отмена), а после завершения команды пересчитывает поля
    /// только у тех из них, что являются панелями PK_Panel.
    /// </summary>
    public static class PanelFieldUpdater
    {
        /// <summary>Наблюдатели по документам.</summary>
        private static readonly Dictionary<Document, DocumentWatcher> Watchers = new Dictionary<Document, DocumentWatcher>();

        /// <summary>Запущено ли обновление.</summary>
        private static bool _started;

        /// <summary>
        /// Запуск: подключение ко всем открытым чертежам и к чертежам, которые откроются позже.
        /// </summary>
        public static void Start()
        {
            if (_started) return;
            _started = true;

            var documents = AcApp.DocumentManager;
            foreach (Document document in documents) Attach(document);
            documents.DocumentCreated += OnDocumentCreated;
            documents.DocumentToBeDestroyed += OnDocumentToBeDestroyed;
        }

        /// <summary>
        /// Остановка: отключение от всех чертежей.
        /// </summary>
        public static void Stop()
        {
            if (!_started) return;
            _started = false;

            var documents = AcApp.DocumentManager;
            documents.DocumentCreated -= OnDocumentCreated;
            documents.DocumentToBeDestroyed -= OnDocumentToBeDestroyed;
            foreach (var watcher in Watchers.Values) watcher.Detach();
            Watchers.Clear();
        }

        /// <summary>Открыт новый чертёж.</summary>
        private static void OnDocumentCreated(object sender, DocumentCollectionEventArgs e) => Attach(e.Document);

        /// <summary>Чертёж закрывается.</summary>
        private static void OnDocumentToBeDestroyed(object sender, DocumentCollectionEventArgs e)
        {
            if (e.Document == null || !Watchers.TryGetValue(e.Document, out var watcher)) return;
            watcher.Detach();
            Watchers.Remove(e.Document);
        }

        /// <summary>Подключение к чертежу (повторное подключение игнорируется).</summary>
        private static void Attach(Document document)
        {
            if (document == null || Watchers.ContainsKey(document)) return;
            Watchers[document] = new DocumentWatcher(document);
        }

        /// <summary>
        /// Наблюдатель одного чертежа.
        /// </summary>
        private sealed class DocumentWatcher
        {
            /// <summary>Чертёж.</summary>
            private readonly Document _document;

            /// <summary>База чертежа (запоминается, чтобы корректно отписаться).</summary>
            private readonly Database _database;

            /// <summary>Вставки блоков, изменённые или добавленные в текущей команде.</summary>
            private readonly HashSet<ObjectId> _pending = new HashSet<ObjectId>();

            /// <summary>Определение PK_Panel в чертеже; пока его нет, изменения не отслеживаются.</summary>
            private ObjectId _definitionId;

            /// <summary>Идёт пересчёт полей — собственные изменения не отслеживаются.</summary>
            private bool _updating;

            /// <summary>Об ошибке уже сообщалось — не засорять командную строку.</summary>
            private bool _errorReported;

            /// <summary>
            /// Подписка на события чертежа.
            /// </summary>
            public DocumentWatcher(Document document)
            {
                _document = document;
                _database = document.Database;
                RefreshDefinitionId();

                _database.ObjectModified += OnObjectChanged;
                _database.ObjectAppended += OnObjectChanged;
                _document.CommandWillStart += OnCommandWillStart;
                _document.CommandEnded += OnCommandFinished;
                _document.CommandCancelled += OnCommandFinished;
                _document.CommandFailed += OnCommandFinished;
                _document.LispEnded += OnLispEnded;
            }

            /// <summary>
            /// Отписка от событий чертежа.
            /// </summary>
            public void Detach()
            {
                _database.ObjectModified -= OnObjectChanged;
                _database.ObjectAppended -= OnObjectChanged;
                _document.CommandWillStart -= OnCommandWillStart;
                _document.CommandEnded -= OnCommandFinished;
                _document.CommandCancelled -= OnCommandFinished;
                _document.CommandFailed -= OnCommandFinished;
                _document.LispEnded -= OnLispEnded;
                _pending.Clear();
            }

            /// <summary>
            /// Изменение или добавление объекта. Выполняется очень часто, поэтому здесь только
            /// запоминается ObjectId вставки блока: без открытия объектов и без транзакций.
            /// </summary>
            private void OnObjectChanged(object sender, ObjectEventArgs e)
            {
                if (_updating || _definitionId.IsNull) return;
                if (e.DBObject is BlockReference reference) _pending.Add(reference.ObjectId);
            }

            /// <summary>
            /// Перед командой: определение блока могло появиться (вставка, копирование из другого чертежа).
            /// </summary>
            private void OnCommandWillStart(object sender, CommandEventArgs e)
            {
                if (_definitionId.IsNull || _definitionId.IsErased) RefreshDefinitionId();
            }

            /// <summary>Команда завершена, отменена или прервана.</summary>
            private void OnCommandFinished(object sender, CommandEventArgs e) => ProcessPending();

            /// <summary>Завершено выражение LISP (оно тоже может менять панели).</summary>
            private void OnLispEnded(object sender, EventArgs e) => ProcessPending();

            /// <summary>
            /// Пересчёт полей у изменённых панелей. Изменение кэша полей не записывается в историю отмены:
            /// иначе Ctrl+Z сначала откатывал бы только марку. После отмены растягивания панель снова
            /// попадает в список изменённых, и марка пересчитывается по восстановленной длине.
            /// </summary>
            private void ProcessPending()
            {
                if (_definitionId.IsNull || _definitionId.IsErased) RefreshDefinitionId();
                if (_pending.Count == 0 || _definitionId.IsNull) { _pending.Clear(); return; }

                var ids = new List<ObjectId>(_pending);
                _pending.Clear();
                _updating = true;
                var undoWasRecording = _database.UndoRecording;
                try
                {
                    using (_document.LockDocument())
                    {
                        if (undoWasRecording) _database.DisableUndoRecording(true);
                        using (var tr = _database.TransactionManager.StartTransaction())
                        {
                            foreach (var id in ids)
                            {
                                if (id.IsNull || id.IsErased || !id.IsValid || id.Database != _database) continue;
                                if (!PanelReader.IsPanel(id, tr)) continue;
                                PanelFieldRefresher.Refresh((BlockReference)tr.GetObject(id, OpenMode.ForRead), tr);
                            }
                            tr.Commit();
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (!_errorReported)
                    {
                        _errorReported = true;
                        _document.Editor.WriteMessage($"\nPanelKomplekt: не удалось обновить марки панелей: {ex.Message}\n");
                    }
                }
                finally
                {
                    if (undoWasRecording && !_database.UndoRecording) _database.DisableUndoRecording(false);
                    _updating = false;
                }
            }

            /// <summary>Поиск определения PK_Panel в чертеже.</summary>
            private void RefreshDefinitionId()
            {
                try { _definitionId = PanelBlock.FindDefinition(_database); }
                catch (Exception) { _definitionId = ObjectId.Null; }
            }
        }
    }
}
