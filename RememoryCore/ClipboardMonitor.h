#pragma once
#include "pch.h"
#include "ClipboardMonitor.g.h"
#include "WindowMessageHook.h"

namespace winrt::RememoryCore::implementation
{
    struct ClipboardData
    {
        LPVOID data = nullptr;
        LPVOID header = nullptr;
        size_t size = 0;
        std::vector<BYTE> hash;

        ClipboardData(const ClipboardData&) = delete;
        ClipboardData& operator=(const ClipboardData&) = delete;

        ClipboardData() = default;
        ~ClipboardData()
        {
            if (data)
            {
                free(data);
            }

            if (header)
            {
                free(header);
            }
        }
    };

    struct ClipboardMonitor : ClipboardMonitorT<ClipboardMonitor>
    {
        ClipboardMonitor();
        ~ClipboardMonitor();

        winrt::hstring HistoryFolderPath() const
        {
            return m_historyFolderPath;
        }
        void HistoryFolderPath(winrt::hstring const& value)
        {
            m_historyFolderPath = value;
        }

        size_t MaxDataSize() const
        {
            return m_maxDataSize;
        }
        void MaxDataSize(size_t const& value)
        {
            m_maxDataSize = value;
        }

        void StartMonitoring(UINT_PTR windowHandle);
        void StopMonitoring();
        bool SetClipboardData(winrt::Windows::Foundation::Collections::IMapView<ClipboardFormat, winrt::hstring> const& dataMap);

        void OnClipboardUpdate();
        void OnWindowDestroy();
        winrt::Windows::Foundation::IAsyncAction HandleClipboardData();

        winrt::event_token ContentDetected(winrt::Windows::Foundation::TypedEventHandler<RememoryCore::ClipboardMonitor, RememoryCore::ClipboardSnapshot> const& handler)
        {
            return m_contentDetectedEvent.add(handler);
        }

        void ContentDetected(winrt::event_token const& token) noexcept
        {
            m_contentDetectedEvent.remove(token);
        }

    private:
        HWND m_hWnd = nullptr;
        DWORD m_oldClipboardSequenceNumber = 0;
        UINT_PTR m_timerId = 0;
        ULONG_PTR m_gdiplusToken = 0;
        std::atomic<bool> m_isMyChanges = false;
        std::unique_ptr<WindowMessageHook> m_message_hook = nullptr;
        std::unordered_map<ClipboardFormat, std::vector<BYTE>> m_previousClipboardDataHashes{};
        winrt::hstring m_lastOwnerPath{};
        winrt::hstring m_historyFolderPath{};
        size_t m_maxDataSize = (size_t)-1;
        winrt::event<winrt::Windows::Foundation::TypedEventHandler<RememoryCore::ClipboardMonitor, RememoryCore::ClipboardSnapshot>> m_contentDetectedEvent;

        static void CALLBACK MonitorTimerProc(HWND hWnd, UINT uMsg, UINT_PTR idEvent, DWORD dwTime);
        static bool CompareClipboardHashes(const std::unordered_map<ClipboardFormat, std::unique_ptr<ClipboardData>>& copiedDataMap, const std::unordered_map<ClipboardFormat, std::vector<BYTE>>& previousHashesMap);

        std::vector<BYTE> ComputeSha256Hash(LPVOID data, size_t dataLength);
        bool TryOpenClipboard();

        void RaiseContentDetected(RememoryCore::ClipboardSnapshot const& snapshot)
        {
            m_contentDetectedEvent(*this, snapshot);
        }
    };
}

namespace winrt::RememoryCore::factory_implementation
{
    struct ClipboardMonitor : ClipboardMonitorT<ClipboardMonitor, implementation::ClipboardMonitor> {};
}
