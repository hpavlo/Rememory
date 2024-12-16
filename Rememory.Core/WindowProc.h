#pragma once

LRESULT CALLBACK MainWindowProc(HWND hwnd, UINT msg, WPARAM wParam, LPARAM lParam);

extern "C" __declspec(dllexport) bool AddWindowProc(HWND hWnd);
