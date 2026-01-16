#include "pch.h"
#include <filesystem>
#include <gdiplus.h>
#include "ClipboardMonitor.h"
#include "ClipboardMonitor.g.cpp"
#include "FormatRecord.h"
#include "ClipboardSnapshot.h"
#include "FormatManager.h"
#include "ProcessInfo.h"
#pragma comment(lib, "gdiplus.lib")

namespace {
    const UINT_PTR TIMER_ID = 1;
    const DWORD TIMER_DELAY = 100;   // 100ms debounce delay
    const UINT OPEN_CLIPBOARD_ATTEMPTS = 5;
    const UINT OPEN_CLIPBOARD_DELAY = 50;   // 50ms wait between attempts
}

namespace winrt::RememoryCore::implementation
{
    ClipboardMonitor::ClipboardMonitor()
    {
        auto appDataLocalFolderPath = std::filesystem::path{ winrt::Microsoft::Windows::Storage::ApplicationData::GetDefault().LocalPath().c_str() };
        auto historyFolderPath = appDataLocalFolderPath / FormatManager::RootHistoryFolderName().c_str();
        HistoryFolderPath({ historyFolderPath.c_str() });

        // Gdiplus used to work with Bitmap
        Gdiplus::GdiplusStartupInput input;
        Gdiplus::GdiplusStartup(&m_gdiplusToken, &input, nullptr);
    }

    ClipboardMonitor::~ClipboardMonitor()
    {
        StopMonitoring();

        if (m_gdiplusToken != 0)
        {
            Gdiplus::GdiplusShutdown(m_gdiplusToken);
            m_gdiplusToken = 0;
        }
    }

    void ClipboardMonitor::StartMonitoring(UINT_PTR windowHandle)
    {
        // Convert the generic LPVOID back to HWND
        HWND new_hWnd = reinterpret_cast<HWND>(windowHandle);

        if (!new_hWnd)
        {
            throw winrt::hresult_invalid_argument(L"Invalid window handle provided.");
        }

        if (m_message_hook)
        {
            // If monitoring is already active, check if it's the same handle.
            if (m_hWnd == new_hWnd)
            {
                return;
            }

            StopMonitoring();
        }

        // Set the new handle
        m_hWnd = new_hWnd;

        try
        {
            m_message_hook = std::make_unique<WindowMessageHook>(m_hWnd, this);
        }
        catch (const std::exception& e)
        {
            m_hWnd = nullptr;
            throw winrt::hresult_error{ E_FAIL, L"Failed to establish Win32 window hook." };
        }
    }

    void ClipboardMonitor::StopMonitoring()
    {
        if (m_timerId) {
            KillTimer(m_hWnd, m_timerId);
            m_timerId = 0;
        }

        if (m_message_hook)
        {
            m_message_hook.reset();
            m_hWnd = nullptr;
        }
    }

    // Timer callback that runs after the debounce delay
    void CALLBACK ClipboardMonitor::MonitorTimerProc(HWND hWnd, UINT uMsg, UINT_PTR idEvent, DWORD dwTime)
    {
        KillTimer(hWnd, idEvent);

        ClipboardMonitor* monitor = WindowMessageHook::GetMonitorInstance(hWnd);

        // Clear the timer ID on the instance
        if (monitor) {
            monitor->m_timerId = 0;
            monitor->HandleClipboardData();
        }
    }

    void ClipboardMonitor::OnClipboardUpdate()
    {
        HWND ownerWindow = GetClipboardOwner();
        m_lastOwnerPath = ProcessInfo::GetProcessPath(reinterpret_cast<UINT_PTR>(ownerWindow));

        DWORD clipboardSequenceNumber = GetClipboardSequenceNumber();
        if (m_oldClipboardSequenceNumber == clipboardSequenceNumber)
        {
            return;
        }
        m_oldClipboardSequenceNumber = clipboardSequenceNumber;

        if (m_isMyChanges.exchange(false)) // Check and clear flag atomically
        {
            return;
        }

        if (!m_timerId)
        {
            // Start timer to debounce multiple rapid updates
            m_timerId = SetTimer(m_hWnd, TIMER_ID, TIMER_DELAY, MonitorTimerProc);
        }
    }

    void ClipboardMonitor::OnWindowDestroy()
    {
        StopMonitoring();
    }


    bool ClipboardMonitor::SetClipboardData(winrt::Windows::Foundation::Collections::IMapView<ClipboardFormat, winrt::hstring> const& dataMap)
    {
        if (dataMap.Size() == 0 || !TryOpenClipboard())
        {
            return false;
        }

        EmptyClipboard();

        for (const auto& [format, rule] : FormatManager::ClipboardFormatRules)
        {
            if (auto data = dataMap.TryLookup(format))
            {
                rule.loadToClipboardFunction(rule.clipboardIds.front(), *data);
            }
        }

        m_isMyChanges = true;
        return CloseClipboard();
    }

    winrt::Windows::Foundation::IAsyncAction ClipboardMonitor::HandleClipboardData()
    {
        if (!TryOpenClipboard())
        {
            co_return;
        }

        std::unordered_map<ClipboardFormat, std::unique_ptr<ClipboardData>> copiedDataMap;

        for (const auto& [format, rule] : FormatManager::ClipboardFormatRules)
        {
            // Save bitmap only if we don't have a png format
            if (format == ClipboardFormat::Bitmap && copiedDataMap.contains(ClipboardFormat::Png))
            {
                continue;
            }

            for (UINT formatId : rule.clipboardIds)
            {
                if (!IsClipboardFormatAvailable(formatId))
                {
                    continue;
                }

                HANDLE hData = GetClipboardData(formatId);
                if (!hData)
                {
                    continue;
                }

                auto copiedData = std::make_unique<ClipboardData>();

                if (!rule.copyFromClipboardFunction(hData, MaxDataSize(), copiedData.get()))
                {
                    continue;
                }

                copiedDataMap.insert_or_assign(format, std::move(copiedData));
                break;   // stop after first successful candidate
            }
        }

        CloseClipboard();

        for (const auto& [_, copiedData] : copiedDataMap)
        {
            copiedData->hash = ComputeSha256Hash(copiedData->data, copiedData->size);
        }

        if (CompareClipboardHashes(copiedDataMap, m_previousClipboardDataHashes))
        {
            co_return;
        }

        auto historyFolderPath = std::filesystem::path{ HistoryFolderPath().c_str() };
        auto records = winrt::single_threaded_vector<RememoryCore::FormatRecord>();

        m_previousClipboardDataHashes.clear();

        for (const auto& [format, copiedData] : copiedDataMap)
        {
            auto formatRule = FormatManager::GetRule(format);
            winrt::hstring dataStr;

            if (formatRule->saveToFileFunction)
            {
                dataStr = co_await formatRule->saveToFileFunction(historyFolderPath, format, copiedData.get());
            }
            else if (copiedData->data && copiedData->size > 0)
            {
                auto* ptr = static_cast<LPWSTR>(copiedData->data);
                uint32_t charCount = static_cast<uint32_t>(copiedData->size / sizeof(WCHAR));

                while (charCount > 0 && ptr[charCount - 1] == L'\0')
                {
                    charCount--;
                }

                dataStr = winrt::hstring{ ptr, charCount };
            }

            if (!dataStr.empty())
            {
                auto hashBuffer = winrt::Windows::Security::Cryptography::CryptographicBuffer::CreateFromByteArray(copiedData->hash);
                auto record = winrt::make<implementation::FormatRecord>();
                record.Format(format);
                record.Data(std::move(dataStr));
                record.Hash(std::move(hashBuffer));
                records.Append(std::move(record));
            }

            m_previousClipboardDataHashes[format] = std::move(copiedData->hash);
        }

        auto snapshot = winrt::make<implementation::ClipboardSnapshot>();
        snapshot.Records(std::move(records));

        if (!m_lastOwnerPath.empty())
        {
            snapshot.OwnerPath(m_lastOwnerPath);

            auto ownerIcon = ProcessInfo::GetProcessIcon(m_lastOwnerPath);
            if (ownerIcon != nullptr && ownerIcon.Length() > 0)
            {
                snapshot.OwnerIcon(ownerIcon);
            }
        }

        RaiseContentDetected(std::move(snapshot));
    }

    bool ClipboardMonitor::TryOpenClipboard()
    {
        for (int i = 0; i < OPEN_CLIPBOARD_ATTEMPTS; i++)
        {
            if (OpenClipboard(m_hWnd))
            {
                return true;
            }
            Sleep(OPEN_CLIPBOARD_DELAY);
        }
        return false;
    }

    std::vector<BYTE> ClipboardMonitor::ComputeSha256Hash(LPVOID data, size_t dataLength)
    {
        if (data == nullptr || dataLength == 0)
        {
            return {};
        }

        HCRYPTPROV hProv = NULL;
        HCRYPTHASH hHash = NULL;

        if (!CryptAcquireContext(&hProv, NULL, NULL, PROV_RSA_AES, CRYPT_VERIFYCONTEXT))
        {
            return {};
        }

        if (!CryptCreateHash(hProv, CALG_SHA_256, 0, 0, &hHash))
        {
            CryptReleaseContext(hProv, 0);
            return {};
        }

        if (!CryptHashData(hHash, static_cast<LPBYTE>(data), dataLength, 0))
        {
            CryptDestroyHash(hHash);
            CryptReleaseContext(hProv, 0);
            return {};
        }

        DWORD hashLen = 0;
        DWORD len = sizeof(DWORD);

        if (!CryptGetHashParam(hHash, HP_HASHSIZE, reinterpret_cast<BYTE*>(&hashLen), &len, 0))
        {
            CryptDestroyHash(hHash);
            CryptReleaseContext(hProv, 0);
            return {};
        }

        std::vector<BYTE> hashBytes(hashLen);
        len = hashLen;

        if (!CryptGetHashParam(hHash, HP_HASHVAL, hashBytes.data(), &len, 0))
        {
            CryptDestroyHash(hHash);
            CryptReleaseContext(hProv, 0);
            return {};
        }

        CryptDestroyHash(hHash);
        CryptReleaseContext(hProv, 0);

        return hashBytes;
    }

    bool ClipboardMonitor::CompareClipboardHashes(
        const std::unordered_map<ClipboardFormat, std::unique_ptr<ClipboardData>>& copiedDataMap,
        const std::unordered_map<ClipboardFormat, std::vector<BYTE>>& previousHashesMap)
    {
        if (copiedDataMap.size() != previousHashesMap.size())
        {
            return false;
        }

        for (const auto& [format, clipboardDataPtr] : copiedDataMap)
        {
            auto previousHashIt = previousHashesMap.find(format);
            if (previousHashIt == previousHashesMap.end())
            {
                return false;
            }

            const std::vector<BYTE>& currentHash = clipboardDataPtr->hash;
            const std::vector<BYTE>& previousHash = previousHashIt->second;

            if (currentHash.size() != previousHash.size())
            {
                return false;
            }

            if (!std::equal(currentHash.begin(), currentHash.end(), previousHash.begin()))
            {
                return false;
            }
        }

        return true;
    }
}
