#pragma once
#include "ClipboardDataHelper.h"

class OwnerHelper
{
public:
	static bool GetOwnerPath(const HWND owner, WCHAR* ownerPath);
	static void LoadOwnerIcon(const WCHAR* ownerPath, ClipboardDataInfo* dataInfo);

private:
	OwnerHelper();
};
