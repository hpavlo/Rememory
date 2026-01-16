#pragma once
#include "pch.h"
#include "ProcessInfo.g.h"

namespace winrt::RememoryCore::implementation
{
	struct ProcessInfo : ProcessInfoT<ProcessInfo>
	{
		static winrt::hstring GetProcessPath(UINT_PTR windowHandle);
		static winrt::Windows::Storage::Streams::IBuffer GetProcessIcon(winrt::hstring const& processPath);
	};
}

namespace winrt::RememoryCore::factory_implementation
{
	struct ProcessInfo : ProcessInfoT<ProcessInfo, implementation::ProcessInfo> {};
}
