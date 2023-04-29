// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

#if UNITY_EDITOR
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Text;

namespace DepictionEngine.Editor
{
    public class LogEntries
    {
        private static Assembly _editorAssembly = typeof(UnityEditor.Editor).Assembly;
        private static Type _logEntryType = _editorAssembly.GetType("UnityEditor.LogEntry");
        private static Type _logEntriesType = _editorAssembly.GetType("UnityEditor.LogEntries");
        private static Type _logEntryStructType = _editorAssembly.GetType("UnityEditor.LogEntryStruct");
        private static Type _consoleWindowType = _editorAssembly.GetType("UnityEditor.ConsoleWindow");
        private static Type _UTF8StringViewType = _editorAssembly.GetType("UnityEditor.LogEntryUTF8StringView");
        private static Type _consoleWindowConstants = _editorAssembly.GetType("UnityEditor.ConsoleWindow+Constants");

        public const int COLLAPSE_FLAG = 1 << 0;
        public const int LOG_FLAG = 1 << 7;
        public const int WARNING_FLAG = 1 << 8;
        public const int ERROR_FLAG = 1 << 9;
        public const int SHOWTIMESTAMP_FLAG = 1 << 10;

        private static object[] _params1 = new object[1];
        private static object[] _params2 = new object[2];
        private static object[] _params3 = new object[3];
        private static object[] _params4 = new object[4];

        private static PropertyInfo _consoleFlagsPropertyInfo = _logEntriesType.GetProperty("consoleFlags", BindingFlags.Public | BindingFlags.Static);
        public static int consoleFlags
        {
            get => (int)_consoleFlagsPropertyInfo.GetValue(null);
            set => _consoleFlagsPropertyInfo.SetValue(null, value);
        }

        private static MethodInfo _setConsoleFlagMethodInfo = _logEntriesType.GetMethod("SetConsoleFlag", BindingFlags.Public | BindingFlags.Static);
        public static void SetConsoleFlag(int bit, bool value)
        {
            _params2[0] = bit;
            _params2[1] = value;
            _setConsoleFlagMethodInfo.Invoke(null, _params2);
        }

        private static MethodInfo _setFilteringMethodInfo = _logEntriesType.GetMethod("SetFilteringText", BindingFlags.Public | BindingFlags.Static);
        public static void SetFilteringText(string filteringText)
        {
            _params1[0] = filteringText;
            _setFilteringMethodInfo.Invoke(null, _params1);
        }

        private static MethodInfo _getFilteringMethodInfo = _logEntriesType.GetMethod("GetFilteringText", BindingFlags.Public | BindingFlags.Static);
        public static string GetFilteringText()
        {
            return (string)_getFilteringMethodInfo.Invoke(null, null);
        }

        private static MethodInfo _startGettingEntriesMethodInfo = _logEntriesType.GetMethod("StartGettingEntries", BindingFlags.Public | BindingFlags.Static);
        public static int StartGettingEntries()
        {
            return (int)_startGettingEntriesMethodInfo.Invoke(null, null);
        }

        private static MethodInfo _endGettingEntriesMethodInfo = _logEntriesType.GetMethod("EndGettingEntries", BindingFlags.Public | BindingFlags.Static);
        public static void EndGettingEntries()
        {
            _endGettingEntriesMethodInfo.Invoke(null, null);
        }

        private static MethodInfo _getEntryInternalMethodInfo = _logEntriesType.GetMethod("GetEntryInternal", BindingFlags.Public | BindingFlags.Static);
        public static bool GetEntryInternal(int row, [Out] object outputEntry)
        {
            _params2[0] = row;
            _params2[1] = outputEntry;
            return (bool)_getEntryInternalMethodInfo.Invoke(null, _params2);
        }

        private static MethodInfo _getLinesAndModeFromEntryInternalMethodInfo = _logEntriesType.GetMethod("GetLinesAndModeFromEntryInternal", BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(int), typeof(int), typeof(int).MakeByRefType(), typeof(string).MakeByRefType() }, null);
        public static void GetLinesAndModeFromEntryInternal(int row, int numberOfLines, ref int mask, [In, Out] ref string outString)
        {
            _params4[0] = row;
            _params4[1] = numberOfLines;
            _params4[2] = mask;
            _params4[3] = outString;
            _getLinesAndModeFromEntryInternalMethodInfo.Invoke(null, _params4);
            mask = (int)_params4[2];
            outString = (string)_params4[3];
        }

        private static MethodInfo _clearMethodInfo = _logEntriesType.GetMethod("Clear", BindingFlags.Public | BindingFlags.Static);
        public static void Clear()
        {
            _clearMethodInfo.Invoke(null, null);
        }

        private static MethodInfo _getEntryRowIndexMethodInfo = _logEntriesType.GetMethod("GetEntryRowIndex", BindingFlags.NonPublic | BindingFlags.Static);
        public static int GetEntryRowIndex(int globalIndex)
        {
            _params1[0] = globalIndex;
            return (int)_getEntryRowIndexMethodInfo.Invoke(null, _params1);
        }

        private static MethodInfo _getCountMethodInfo = _logEntriesType.GetMethod("GetCount", BindingFlags.Public | BindingFlags.Static);
        public static int GetCount()
        {
            return (int)_getCountMethodInfo.Invoke(null, null);
        }

        private static MethodInfo _getCountsByTypeMethodInfo = _logEntriesType.GetMethod("GetCountsByType", BindingFlags.Public | BindingFlags.Static);
        public static void GetCountsByType(ref int errorCount, ref int warningCount, ref int logCount)
        {
            _params3[0] = errorCount;
            _params3[1] = warningCount;
            _params3[2] = logCount;
            _getCountsByTypeMethodInfo.Invoke(null, _params3);
            errorCount = (int)_params3[0];
            warningCount = (int)_params3[1];
            logCount = (int)_params3[2];
        }

        private static FieldInfo _getEntryMessageFieldInfo = _logEntryType.GetField("message", BindingFlags.Instance | BindingFlags.Public);
        private static string GetLogEntryMessage(object logEntry) { return (string)_getEntryMessageFieldInfo.GetValue(logEntry); }
        private static FieldInfo _getEntryFileFieldInfo = _logEntryType.GetField("file", BindingFlags.Instance | BindingFlags.Public);
        private static string GetLogEntryFile(object logEntry) { return (string)_getEntryFileFieldInfo.GetValue(logEntry); }
        private static FieldInfo _getEntryLineFieldInfo = _logEntryType.GetField("line", BindingFlags.Instance | BindingFlags.Public);
        private static int GetLogEntryLine(object logEntry) { return (int)_getEntryLineFieldInfo.GetValue(logEntry); }
        private static FieldInfo _getEntryColumnFieldInfo = _logEntryType.GetField("column", BindingFlags.Instance | BindingFlags.Public);
        private static int GetLogEntryColumn(object logEntry) { return (int)_getEntryColumnFieldInfo.GetValue(logEntry); }
        private static FieldInfo _getEntryInstanceIDFieldInfo = _logEntryType.GetField("instanceID", BindingFlags.Instance | BindingFlags.Public);
        private static int GetLogEntryInstanceID(object logEntry) { return (int)_getEntryInstanceIDFieldInfo.GetValue(logEntry); }
        private static FieldInfo _getEntryIdentifierFieldInfo = _logEntryType.GetField("identifier", BindingFlags.Instance | BindingFlags.Public);
        private static int GetLogEntryIdentifier(object logEntry) { return (int)_getEntryIdentifierFieldInfo.GetValue(logEntry); }
        private static FieldInfo _getEntryCallstackTextStartUTF8FieldInfo = _logEntryType.GetField("callstackTextStartUTF8", BindingFlags.Instance | BindingFlags.Public);
        private static int GetLogEntryCallstackTextStartUTF8(object logEntry) { return (int)_getEntryCallstackTextStartUTF8FieldInfo.GetValue(logEntry); }

        private static FieldInfo _setEntryTimestampFieldInfo = _logEntryStructType.GetField("timestamp", BindingFlags.Instance | BindingFlags.Public);
        private static void SetLogEntryStructTimestamp(object logEntryStruct, object value) { _setEntryTimestampFieldInfo.SetValue(logEntryStruct, value); }
        private static FieldInfo _setEntryMessageFieldInfo = _logEntryStructType.GetField("message", BindingFlags.Instance | BindingFlags.Public);
        private static void SetLogEntryStructMessage(object logEntryStruct, object value) { _setEntryMessageFieldInfo.SetValue(logEntryStruct, value); }
        private static FieldInfo _setEntryCallstackFieldInfo = _logEntryStructType.GetField("callstack", BindingFlags.Instance | BindingFlags.Public);
        private static void SetLogEntryStructCallstack(object logEntryStruct, object value) { _setEntryCallstackFieldInfo.SetValue(logEntryStruct, value); }
        private static FieldInfo _setEntryFileFieldInfo = _logEntryStructType.GetField("file", BindingFlags.Instance | BindingFlags.Public);
        private static void SetLogEntryStructFile(object logEntryStruct, object value) { _setEntryFileFieldInfo.SetValue(logEntryStruct, value); }
        private static FieldInfo _setEntryLineFieldInfo = _logEntryStructType.GetField("line", BindingFlags.Instance | BindingFlags.Public);
        private static void SetLogEntryStructLine(object logEntryStruct, object value) { _setEntryLineFieldInfo.SetValue(logEntryStruct, value); }
        private static FieldInfo _setEntryColumnFieldInfo = _logEntryStructType.GetField("column", BindingFlags.Instance | BindingFlags.Public);
        private static void SetLogEntryStructColumn(object logEntryStruct, object value) { _setEntryColumnFieldInfo.SetValue(logEntryStruct, value); }
        private static FieldInfo _setEntryModeFieldInfo = _logEntryStructType.GetField("mode", BindingFlags.Instance | BindingFlags.Public);
        private static void SetLogEntryStructMode(object logEntryStruct, object value) { _setEntryModeFieldInfo.SetValue(logEntryStruct, value); }
        private static FieldInfo _setEntryInstanceIDFieldInfo = _logEntryStructType.GetField("instanceID", BindingFlags.Instance | BindingFlags.Public);
        private static void SetLogEntryStructInstanceID(object logEntryStruct, object value) { _setEntryInstanceIDFieldInfo.SetValue(logEntryStruct, value); }
        private static FieldInfo _setEntryIdentifierFieldInfo = _logEntryStructType.GetField("identifier", BindingFlags.Instance | BindingFlags.Public);
        private static void SetLogEntryStructIdentifier(object logEntryStruct, object value) { _setEntryIdentifierFieldInfo.SetValue(logEntryStruct, value); }

        public static void GetConsoleFlags(ref string filteringText, ref bool collapse, ref bool log, ref bool warning, ref bool error, ref bool showTimestamp)
        {
            filteringText = GetFilteringText();
            int localConsoleFlags = consoleFlags;
            collapse = HasFlag(localConsoleFlags, COLLAPSE_FLAG);
            log = HasFlag(localConsoleFlags, LOG_FLAG);
            warning = HasFlag(localConsoleFlags, WARNING_FLAG);
            error = HasFlag(localConsoleFlags, ERROR_FLAG);
            showTimestamp = HasFlag(localConsoleFlags, SHOWTIMESTAMP_FLAG);
        }

        public static bool HasFlag(int consoleFlags, int flags) { return (consoleFlags & flags) != 0; }

        //ConsoleWindow
        private static MethodInfo _showConsoleRowMethodInfo = _consoleWindowType.GetMethod("ShowConsoleRow", BindingFlags.NonPublic | BindingFlags.Static);
        public static void ShowConsoleRow(int row)
        {
            _params1[0] = row;
            _showConsoleRowMethodInfo.Invoke(null, _params1);
        }

        private static PropertyInfo _consoleWindowLogStyleLineCountPropertyInfo = _consoleWindowConstants.GetProperty("LogStyleLineCount");
        public static int LogStyleLineCount
        {
            get { return (int)_consoleWindowLogStyleLineCountPropertyInfo.GetValue(null); }
        }

        private static MethodInfo _consoleWindowAddMessageMethodInfo = _consoleWindowType.GetMethod("AddMessage", BindingFlags.NonPublic | BindingFlags.Static);
        public static void AddMessage(ref object message)
        {
            _params1[0] = message;
            _consoleWindowAddMessageMethodInfo.Invoke(null, _params1);
        }

        private static MethodInfo _consoleWindowSetActiveEntryMethodInfo = _consoleWindowType.GetMethod("SetActiveEntry", BindingFlags.NonPublic | BindingFlags.Instance);
        public static void SetActiveEntry(object consoleWindow, object entry)
        {
            _params1[0] = entry;
            _consoleWindowRestoreLastActiveEntryMethodInfo.Invoke(consoleWindow, _params1);
        }

        private static MethodInfo _consoleWindowRestoreLastActiveEntryMethodInfo = _consoleWindowType.GetMethod("RestoreLastActiveEntry", BindingFlags.NonPublic | BindingFlags.Instance);
        public static void RestoreLastActiveEntry(object consoleWindow)
        {
            _consoleWindowRestoreLastActiveEntryMethodInfo.Invoke(consoleWindow, null);
        }

        private static FieldInfo _ms_ConsoleWindowFieldInfo = _consoleWindowType.GetField("ms_ConsoleWindow", BindingFlags.NonPublic | BindingFlags.Static);
        public static object ms_ConsoleWindow
        {
            get { return _ms_ConsoleWindowFieldInfo.GetValue(null); }
        }

        private static FieldInfo _m_LastActiveEntryIndexFieldInfo = _consoleWindowType.GetField("m_LastActiveEntryIndex", BindingFlags.NonPublic | BindingFlags.Instance);
        public static int m_LastActiveEntryIndex
        {
            get { return ms_ConsoleWindow != null ? (int)_m_LastActiveEntryIndexFieldInfo.GetValue(ms_ConsoleWindow) : -1; }
        }

        private static FieldInfo _m_ListViewFieldInfo = _consoleWindowType.GetField("m_ListView", BindingFlags.Instance | BindingFlags.NonPublic);
        public static object m_ListView
        {
            get { return ms_ConsoleWindow != null ? _m_ListViewFieldInfo.GetValue(ms_ConsoleWindow) : null; }
        }

        private static FieldInfo _selectedItemsFieldInfo;
        public static void SetSelectedItems(object listView, bool[] selectedItems)
        {
            if (_selectedItemsFieldInfo == null)
                _selectedItemsFieldInfo = listView.GetType().GetField("selectedItems", BindingFlags.Instance | BindingFlags.Public);
            _selectedItemsFieldInfo.SetValue(listView, selectedItems);
        }

        private static object _logEntry = Activator.CreateInstance(_logEntryType);
        private static object _logEntryStruct = Activator.CreateInstance(_logEntryStructType);
        private static StringBuilder _stringBuilder = new StringBuilder();
        public static int RemoveEntries(HashSet<int> rows)
        {
            if (rows == null || rows.Count == 0)
                return 0;
            
            int removedCount = 0;

            string filteringText = null; bool collapse = false; bool log = false; bool warning = false; bool error = false; bool showTimestamp = false;
            GetConsoleFlags(ref filteringText, ref collapse, ref log, ref warning, ref error, ref showTimestamp);

            SetFilteringText("");
            SetConsoleFlag(COLLAPSE_FLAG, false);
            SetConsoleFlag(SHOWTIMESTAMP_FLAG, true);
            SetConsoleFlag(LOG_FLAG, true);
            SetConsoleFlag(WARNING_FLAG, true);
            SetConsoleFlag(ERROR_FLAG, true);

            int entriesCount = StartGettingEntries();

            string[] timestamps = new string[entriesCount];
            string[] messages = new string[entriesCount];
            int[] modes = new int[entriesCount];
            string[] files = new string[entriesCount];
            int[] lines = new int[entriesCount];
            int[] columns = new int[entriesCount];
            int[] instanceIds = new int[entriesCount];
            int[] identifiers = new int[entriesCount];
            int[] callstackTextStartUTF8s = new int[entriesCount];

            for (int i = 0; i < entriesCount; i++)
            {
                if (GetEntryInternal(i, _logEntry))
                {
                    string message = GetLogEntryMessage(_logEntry);
                    if (!rows.Contains(i))
                    {
                        int mode = 0;
                        string messageFirstLine = "";
                        GetLinesAndModeFromEntryInternal(i, 1, ref mode, ref messageFirstLine);

                        _stringBuilder.Clear();

                        timestamps[i] = _stringBuilder.Append(messageFirstLine).ToString(0, 10);
                        messages[i] = message;
                        modes[i] = mode;
                        files[i] = GetLogEntryFile(_logEntry);
                        lines[i] = GetLogEntryLine(_logEntry);
                        columns[i] = GetLogEntryColumn(_logEntry);
                        instanceIds[i] = GetLogEntryInstanceID(_logEntry);
                        identifiers[i] = GetLogEntryIdentifier(_logEntry);
                        callstackTextStartUTF8s[i] = GetLogEntryCallstackTextStartUTF8(_logEntry);
                    }
                    else
                        removedCount++;
                }
            }

            Clear();

            int startIndex;
            int charCount;
            for (int i = 0; i < entriesCount; i++)
            {
                string timestamp = timestamps[i];
                if (timestamp != null)
                {
                    SetLogEntryStructTimestamp(_logEntryStruct, ReadString(timestamp));

                    string message = messages[i];

                    startIndex = 0;
                    charCount = callstackTextStartUTF8s[i];
                    SetLogEntryStructMessage(_logEntryStruct, ReadString(message, startIndex, charCount));

                    startIndex += charCount + 1;
                    SetLogEntryStructCallstack(_logEntryStruct, ReadString(message, startIndex));

                    string file = files[i];
                    SetLogEntryStructFile(_logEntryStruct, ReadString(file));

                    SetLogEntryStructLine(_logEntryStruct, lines[i]);
                    SetLogEntryStructColumn(_logEntryStruct, columns[i]);
                    SetLogEntryStructMode(_logEntryStruct, modes[i]);
                    SetLogEntryStructInstanceID(_logEntryStruct, instanceIds[i]);
                    SetLogEntryStructIdentifier(_logEntryStruct, identifiers[i]);

                    AddMessage(ref _logEntryStruct);
                }
            }

            static object ReadString(string managedString, int index = 0, int count = -1)
            {
                if (count < 0)
                    count = managedString.Length - index;
                if (count != 0)
                {
                    try
                    {
                        int len = Encoding.UTF8.GetByteCount(managedString, index, count);
                        if (len != 0)
                        {
                            byte[] bytes = new byte[len + 1];
                            Encoding.UTF8.GetBytes(managedString, index, count, bytes, bytes.GetLowerBound(0));

                            unsafe
                            {
                                fixed (byte* ptr = &bytes[0])
                                {
                                    return Activator.CreateInstance(_UTF8StringViewType, (IntPtr)ptr, len);
                                }
                            }
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
                return Activator.CreateInstance(_UTF8StringViewType);
            }

            EndGettingEntries();

            SetFilteringText(filteringText);
            SetConsoleFlag(SHOWTIMESTAMP_FLAG, showTimestamp);
            SetConsoleFlag(COLLAPSE_FLAG, collapse);
            SetConsoleFlag(LOG_FLAG, log);
            SetConsoleFlag(WARNING_FLAG, warning);
            SetConsoleFlag(ERROR_FLAG, error);

            //Update selected items
            object listView = m_ListView;
            if (listView != null)
            {
                int rowIndex = GetEntryRowIndex(m_LastActiveEntryIndex);
                if (rowIndex != -1)
                {
                    ShowConsoleRow(rowIndex);

                    bool[] selectedItems = new bool[rowIndex + 1];
                    selectedItems[rowIndex] = true;
                    SetSelectedItems(listView, selectedItems);
                }
            }

            return removedCount;
        }
    }
}
#endif
