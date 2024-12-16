#include "pch.h"
#include "ClipboardManager.h"
#include "OwnerHelper.h"
#include "HashHelper.h"
//#include <iostream>

const UINT TIMER_DELAY = 100;
const UINT OPEN_CLIPBOARD_ATTEMPTS = 3;
const UINT OPEN_CLIPBOARD_DELAY = 50;

std::vector<FormatDataItem> ClipboardData;
std::vector<FormatDataItem> CopiedClipboardData;

#pragma region External functions

bool StartClipboardMonitor(HWND hWnd, Callback handler)
{
    //AllocConsole();
    //FILE* fp;
    //freopen_s(&fp, "CONOUT$", "w", stdout);

    return ClipboardManager::GetInstance().StartMonitoring(hWnd, handler);
}

bool StopClipboardMonitor(HWND hWnd)
{
    //FreeConsole();
    return ClipboardManager::GetInstance().StopMonitoring();
}

bool SetDataToClipboard(RtfPreviewInfo& dataInfo)
{
    return ClipboardManager::GetInstance().SetDataToClipboard(dataInfo);
}

#pragma endregion

ClipboardManager& ClipboardManager::GetInstance()
{
    static ClipboardManager instance;
    return instance;
}

bool ClipboardManager::StartMonitoring(HWND hWnd, Callback handler)
{
    _hWnd = hWnd;
    _handler = handler;
    _ownerPath = new WCHAR[MAX_PATH];
    _ownerPath[0] = '\0';

    ClipboardDataHelper::InitializeClipboardFormats();

    _oldClipboardSequenceNumber = GetClipboardSequenceNumber();
    _nextClipboardViewer = SetClipboardViewer(_hWnd);
    return true;
}

bool ClipboardManager::StopMonitoring()
{
    if (_timerId != 0)
    {
        KillTimer(_hWnd, _timerId);
    }
    ChangeClipboardChain(_hWnd, _nextClipboardViewer);
    delete[] _ownerPath;
    return true;
}

bool ClipboardManager::SetDataToClipboard(RtfPreviewInfo& dataInfo)
{
    if (!dataInfo.formatCount || !TryOpenClipboard())
    {
        return false;
    }
    EmptyClipboard();

    for (int i = 0; i < dataInfo.formatCount; i++)
    {
        FormatDataItem* formatDataItem = dataInfo.firstItem + i;
        SetClipboardData(formatDataItem->format, formatDataItem->data);
    }

    _isMyChanges = true;
    return CloseClipboard();
}

void ClipboardManager::ClipboardManagerMessage(HWND hWnd, UINT uMsg, WPARAM wParam, LPARAM lParam)
{
    switch (uMsg)
    {
    case WM_DRAWCLIPBOARD:
    {
        OwnerHelper::GetOwnerPath((HWND)wParam, _ownerPath);

        DWORD clipboardSequenceNumber = GetClipboardSequenceNumber();
        if (_oldClipboardSequenceNumber == clipboardSequenceNumber)
        {
            break;
        }
        _oldClipboardSequenceNumber = clipboardSequenceNumber;

        if (_isMyChanges)
        {
            _isMyChanges = false;
            break;
        }

        if (!_timerId)
        {
            _timerId = SetTimer(_hWnd, 1, TIMER_DELAY, MonitorTimerProc);
        }

        if (_nextClipboardViewer != NULL)
        {
            SendMessage(_nextClipboardViewer, uMsg, wParam, lParam);
        }
        break;
    }
    case WM_CHANGECBCHAIN:
    {
        if ((HWND)wParam == _nextClipboardViewer)
        {
            _nextClipboardViewer = (HWND)lParam;
        }
        else if (_nextClipboardViewer != NULL)
        {
            SendMessage(_nextClipboardViewer, uMsg, wParam, lParam);
        }
        break;
    }
    }
}

void CALLBACK ClipboardManager::MonitorTimerProc(HWND hWnd, UINT uMsg, UINT_PTR idEvent, DWORD dwTime)
{
    KillTimer(hWnd, idEvent);
    ClipboardManager& monitor = ClipboardManager::GetInstance();
    monitor._timerId = 0;
    monitor.HandleClipboardData();
}

void ClipboardManager::HandleClipboardData()
{
    if (!TryOpenClipboard())
    {
        return;
    }

    UINT format = 0;
    while ((format = EnumClipboardFormats(format)) != 0)
    {
        if (!ClipboardDataHelper::IsFormatSupported(format) || ClipboardDataHelper::GetDataByFormat(ClipboardData, format))
        {
            continue;
        }

        HANDLE hData = GetClipboardData(format);
        if (!hData)
        {
            continue;
        }

        ClipboardData.push_back(FormatDataItem(format, hData));
    }

    bool hasNewData = !ClipboardData.empty() && !ClipboardDataHelper::CompareClipboardData(CopiedClipboardData, ClipboardData);
    if (hasNewData) {
        ClipboardDataHelper::MakeDataCopy(CopiedClipboardData, ClipboardData);
    }

    CloseClipboard();
    ClipboardData.clear();

    if (hasNewData) {
        RtfPreviewInfo dataInfo = {};
        dataInfo.formatCount = CopiedClipboardData.size();
        dataInfo.firstItem = CopiedClipboardData.data();

        if (_ownerPath[0])
        {
            dataInfo.ownerPath = _ownerPath;
            OwnerHelper::LoadOwnerIcon(_ownerPath, &dataInfo);
        }

        _handler(dataInfo);

        if (dataInfo.iconPixels) {
            free(dataInfo.iconPixels);
        }
    }
}

bool ClipboardManager::TryOpenClipboard()
{
    for (int i = 0; i < OPEN_CLIPBOARD_ATTEMPTS; i++)
    {
        if (OpenClipboard(_hWnd))
        {
            return true;
        }
        Sleep(OPEN_CLIPBOARD_DELAY);
    }
    return false;
}
