#include "pch.h"
#include "WindowMessageHook.h"
#include "ClipboardMonitor.h"

std::map<HWND, WindowMessageHook*> WindowMessageHook::s_hook_map;

ParentMonitor* WindowMessageHook::GetMonitorInstance(HWND hWnd)
{
    WindowMessageHook* messageHook = GetHookInstance(hWnd);

    if (messageHook)
    {
        return messageHook->m_parentMonitor;
    }

    return nullptr;
}

WindowMessageHook* WindowMessageHook::GetHookInstance(HWND hWnd)
{
    auto it = s_hook_map.find(hWnd);

    if (it != s_hook_map.end())
    {
        return it->second;
    }

    return nullptr;
}

WindowMessageHook::WindowMessageHook(HWND hWnd, ParentMonitor* parentMonitor)
    : m_hWnd(hWnd), m_parentMonitor(parentMonitor)
{
    if (!m_hWnd || !m_parentMonitor)
    {
        throw std::invalid_argument{ "HWND and ParentMonitor cannot be null." };
    }

    s_hook_map[m_hWnd] = this;

    // We store the original procedure to allow message chaining.
    m_originalProc = (WNDPROC)SetWindowLongPtr(m_hWnd, GWLP_WNDPROC, (LONG_PTR)StaticMessageProc);

    if (!m_originalProc)
    {
        s_hook_map.erase(m_hWnd);
        throw std::runtime_error{ "Failed to subclass the window (SetWindowLongPtr failed)." };
    }

    if (!AddClipboardFormatListener(m_hWnd))
    {
        auto error = GetLastError();
        // Failed to start listener; clean up the subclassing before throwing
        CleanupHook();
        throw std::runtime_error{ "Failed to start clipboard format listener." };
    }
}

WindowMessageHook::~WindowMessageHook()
{
    CleanupHook();
}

LRESULT CALLBACK WindowMessageHook::StaticMessageProc(HWND hWnd, UINT uMsg, WPARAM wParam, LPARAM lParam)
{
    WindowMessageHook* messageHook = GetHookInstance(hWnd);

    if (messageHook)
    {
        return messageHook->MessageProc(hWnd, uMsg, wParam, lParam);
    }

    return DefWindowProc(hWnd, uMsg, wParam, lParam);
}

LRESULT WindowMessageHook::MessageProc(HWND hWnd, UINT uMsg, WPARAM wParam, LPARAM lParam)
{
    WNDPROC originalProc = m_originalProc;

    switch (uMsg)
    {
    case WM_CLIPBOARDUPDATE:
        m_parentMonitor->OnClipboardUpdate();
        break;

    case WM_DESTROY:
        CleanupHook();
        m_parentMonitor->OnWindowDestroy();
        return CallWindowProc(originalProc, hWnd, uMsg, wParam, lParam);
    }

    return CallWindowProc(m_originalProc, hWnd, uMsg, wParam, lParam);
}

void WindowMessageHook::CleanupHook() noexcept
{
    if (m_hWnd)
    {
        RemoveClipboardFormatListener(m_hWnd);

        if (m_originalProc)
        {
            SetWindowLongPtr(m_hWnd, GWLP_WNDPROC, (LONG_PTR)m_originalProc);
            m_originalProc = nullptr;
        }

        s_hook_map.erase(m_hWnd);
        m_hWnd = nullptr;
    }
}
