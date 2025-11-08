#pragma once
#include "ClipboardDataHelper.h"

extern "C" __declspec(dllexport) void GetOwnerIcon(const WCHAR* ownerPath, LONG* iconLength, BYTE** iconPixels);
extern "C" __declspec(dllexport) void FreeOwnerIcon(BYTE** iconPixels);

class OwnerHelper
{
public:
	static bool GetOwnerPath(const HWND owner, WCHAR* ownerPath);
	static void LoadOwnerIcon(const WCHAR* ownerPath, LONG* iconLength, BYTE** iconPixels);

private:
	OwnerHelper();
};
