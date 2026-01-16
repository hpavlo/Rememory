#pragma once
#include <map>

// Forward declaration of the C++/WinRT implementation class
namespace winrt::RememoryCore::implementation
{
    struct ClipboardMonitor;
}

using ParentMonitor = winrt::RememoryCore::implementation::ClipboardMonitor;

class WindowMessageHook
{
public:
    // Provides encapsulated access to the associated ParentMonitor instance.
    static ParentMonitor* GetMonitorInstance(HWND hWnd);

    WindowMessageHook(HWND hWnd, ParentMonitor* parentMonitor);
    ~WindowMessageHook();

    WindowMessageHook(const WindowMessageHook&) = delete;
    WindowMessageHook& operator=(const WindowMessageHook&) = delete;

private:
    // Static map to associate the window handle (HWND) with the correct hook instance.
    static std::map<HWND, WindowMessageHook*> s_hook_map;

    HWND m_hWnd = nullptr;
    ParentMonitor* m_parentMonitor = nullptr;
    WNDPROC m_originalProc = nullptr;

    static WindowMessageHook* GetHookInstance(HWND hWnd);
    static LRESULT CALLBACK StaticMessageProc(HWND hWnd, UINT uMsg, WPARAM wParam, LPARAM lParam);

    LRESULT MessageProc(HWND hWnd, UINT uMsg, WPARAM wParam, LPARAM lParam);
    void CleanupHook() noexcept;
};
