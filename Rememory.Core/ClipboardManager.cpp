#include "pch.h"
#include "ClipboardManager.h"
#include "OwnerHelper.h"
#include "HashHelper.h"

const UINT TIMER_DELAY = 100;
const UINT OPEN_CLIPBOARD_ATTEMPTS = 3;
const UINT OPEN_CLIPBOARD_DELAY = 50;

std::vector<FormatDataItem> ClipboardData;
std::vector<FormatDataItem> CopiedClipboardData;

#pragma region External functions

bool StartClipboardMonitor(HWND hWnd, Callback handler)
{
    return ClipboardManager::GetInstance().StartMonitoring(hWnd, handler);
}

bool StopClipboardMonitor(HWND hWnd)
{
    return ClipboardManager::GetInstance().StopMonitoring();
}

bool SetDataToClipboard(ClipboardDataInfo& dataInfo)
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
    return AddClipboardFormatListener(_hWnd);
}

bool ClipboardManager::StopMonitoring()
{
    if (_timerId != 0)
    {
        KillTimer(_hWnd, _timerId);
    }
    if (_ownerPath) {
        delete[] _ownerPath;
        _ownerPath = nullptr;
    }
    return RemoveClipboardFormatListener(_hWnd);
}

bool ClipboardManager::SetDataToClipboard(ClipboardDataInfo& dataInfo)
{
    if (!dataInfo.formatCount || !TryOpenClipboard())
    {
        return false;
    }
    EmptyClipboard();

    for (int i = 0; i < dataInfo.formatCount; i++)
    {
        FormatDataItem* formatDataItem = dataInfo.firstItem + i;

        if (formatDataItem->format == CF_BITMAP)
        {
            WCHAR* path = static_cast<WCHAR*>(formatDataItem->data);
            HBITMAP hBitmap = (HBITMAP)LoadImage(NULL, path, IMAGE_BITMAP, 0, 0, LR_DEFAULTSIZE | LR_LOADFROMFILE);
            
            if (hBitmap)
            {
                SetClipboardData(formatDataItem->format, hBitmap);
            }
        }
        else SetClipboardData(formatDataItem->format, formatDataItem->data);
    }

    _isMyChanges = true;
    return CloseClipboard();
}

void ClipboardManager::ClipboardManagerMessage(HWND hWnd, UINT uMsg, WPARAM wParam, LPARAM lParam)
{
    switch (uMsg)
    {
    case WM_CLIPBOARDUPDATE:
    {
        OwnerHelper::GetOwnerPath(GetClipboardOwner(), _ownerPath);

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

#define PNG_FORMAT RegisterClipboardFormat(L"PNG")
#define IMAGE_PNG_FORMAT RegisterClipboardFormat(L"image/png")

void ClipboardManager::HandleClipboardData()
{
    if (!TryOpenClipboard())
    {
        return;
    }

    bool isPngAvailable = IsClipboardFormatAvailable(PNG_FORMAT);
    bool isImagePngAvailable = IsClipboardFormatAvailable(IMAGE_PNG_FORMAT);

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

        // Save bitmap only if we don't have a png format
        if (format == CF_BITMAP) {
            if (isPngAvailable || isImagePngAvailable)
                continue;

            // Create FormatDataItem manually to avoid GlobalSize calculating
            FormatDataItem fdi = {};
            fdi.format = format;
            fdi.data = hData;

            ClipboardData.push_back(fdi);
            continue;
        }

        if (format == IMAGE_PNG_FORMAT && !isPngAvailable) {
            // Put "image/png" as "PNG" format
            ClipboardData.push_back(FormatDataItem(PNG_FORMAT, hData));
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
        ClipboardDataInfo dataInfo = {};
        dataInfo.formatCount = CopiedClipboardData.size();
        dataInfo.firstItem = CopiedClipboardData.data();

        dataInfo.iconLength = 0;
        dataInfo.iconPixels = nullptr;

        if (_ownerPath[0])
        {
            dataInfo.ownerPath = _ownerPath;
            OwnerHelper::LoadOwnerIcon(_ownerPath, &dataInfo.iconLength, &dataInfo.iconPixels);
        }

        _handler(&dataInfo);

        if (dataInfo.iconPixels) {
            free(dataInfo.iconPixels);
            dataInfo.iconPixels = nullptr;
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
