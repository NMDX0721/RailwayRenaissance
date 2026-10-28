using System;
using System.Collections.Generic;

namespace RailwayRenaissance.UI.Tablet.SystemShell
{
    public enum TabletSystemPanel
    {
        None,
        Notifications,
        Controls
    }

    public sealed class TabletNotification
    {
        public long Id { get; }
        public string Title { get; }
        public string Body { get; }
        public DateTime CreatedAt { get; }
        public bool IsRead { get; internal set; }

        public TabletNotification(long id, string title, string body, DateTime createdAt, bool isRead = false)
        {
            Id = id;
            Title = title ?? string.Empty;
            Body = body ?? string.Empty;
            CreatedAt = createdAt;
            IsRead = isRead;
        }
    }

    public sealed class TabletShellState
    {
        public const int MaxCapacity = 32;
        private readonly List<TabletNotification> _notifications = new List<TabletNotification>();
        private readonly System.Collections.ObjectModel.ReadOnlyCollection<TabletNotification> _readOnlyNotifications;
        private long _nextId = 1;

        public event Action Changed;

        public TabletSystemPanel Panel { get; private set; } = TabletSystemPanel.None;
        public IReadOnlyList<TabletNotification> Notifications => _readOnlyNotifications;
        public bool QuietMode { get; private set; }
        public TabletNotificationStorage Storage { get; private set; }

        public int UnreadCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _notifications.Count; i++)
                {
                    if (!_notifications[i].IsRead) count++;
                }
                return count;
            }
        }

        public TabletShellState(string storagePath = null)
        {
            _readOnlyNotifications = _notifications.AsReadOnly();
            if (!string.IsNullOrEmpty(storagePath))
            {
                InitializeStorage(storagePath);
            }
        }

        public TabletShellState(TabletNotificationStorage storage)
        {
            _readOnlyNotifications = _notifications.AsReadOnly();
            if (storage != null)
            {
                AttachStorage(storage);
            }
        }

        public void InitializeStorage(string storagePath)
        {
            if (string.IsNullOrEmpty(storagePath)) return;
            AttachStorage(new TabletNotificationStorage(storagePath));
        }

        public void AttachStorage(TabletNotificationStorage storage)
        {
            Storage = storage;
            LoadFromStorage();
        }

        private void LoadFromStorage()
        {
            if (Storage == null) return;
            if (Storage.Load(out bool quiet, out var loadedNotifs, out long nextId))
            {
                QuietMode = quiet;
                _notifications.Clear();
                if (loadedNotifs != null)
                {
                    _notifications.AddRange(loadedNotifs);
                }
                _nextId = nextId;
            }
        }

        private void SaveToStorage()
        {
            Storage?.Save(QuietMode, _notifications);
        }

        public void TogglePanel(TabletSystemPanel panel)
        {
            if (!Enum.IsDefined(typeof(TabletSystemPanel), panel)) throw new ArgumentOutOfRangeException(nameof(panel));
            if (panel == TabletSystemPanel.None || Panel == panel)
            {
                ClosePanel();
                return;
            }

            Panel = panel;
            if (Panel == TabletSystemPanel.Notifications)
            {
                MarkAllAsReadInternal();
            }
            Changed?.Invoke();
        }

        public bool ClosePanel()
        {
            if (Panel == TabletSystemPanel.None)
            {
                return false;
            }

            Panel = TabletSystemPanel.None;
            Changed?.Invoke();
            return true;
        }

        public void SetQuietMode(bool enabled)
        {
            if (QuietMode == enabled)
            {
                return;
            }

            QuietMode = enabled;
            SaveToStorage();
            Changed?.Invoke();
        }

        public long Post(string title, string body)
        {
            string cleanTitle = title?.Trim() ?? string.Empty;
            string cleanBody = body?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(cleanTitle) && string.IsNullOrEmpty(cleanBody))
            {
                return -1;
            }

            if (string.IsNullOrEmpty(cleanTitle))
            {
                cleanTitle = "系统提醒";
            }

            long id = _nextId++;
            var notification = new TabletNotification(id, cleanTitle, cleanBody, DateTime.Now, isRead: false);
            _notifications.Insert(0, notification);

            while (_notifications.Count > MaxCapacity)
            {
                _notifications.RemoveAt(_notifications.Count - 1);
            }

            SaveToStorage();
            Changed?.Invoke();
            return id;
        }

        public bool Dismiss(long id)
        {
            for (int i = 0; i < _notifications.Count; i++)
            {
                if (_notifications[i].Id == id)
                {
                    _notifications.RemoveAt(i);
                    SaveToStorage();
                    Changed?.Invoke();
                    return true;
                }
            }

            return false;
        }

        public void ClearNotifications()
        {
            if (_notifications.Count == 0)
            {
                return;
            }

            _notifications.Clear();
            SaveToStorage();
            Changed?.Invoke();
        }

        public bool MarkAllAsRead()
        {
            bool modified = MarkAllAsReadInternal();
            if (modified)
            {
                Changed?.Invoke();
            }
            return modified;
        }

        private bool MarkAllAsReadInternal()
        {
            bool modified = false;
            for (int i = 0; i < _notifications.Count; i++)
            {
                if (!_notifications[i].IsRead)
                {
                    _notifications[i].IsRead = true;
                    modified = true;
                }
            }
            if (modified)
            {
                SaveToStorage();
            }
            return modified;
        }
    }
}
