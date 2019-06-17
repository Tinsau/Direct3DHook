#pragma once

#include <Windows.h>

#ifdef WINDOWFINDERDLG_EXPORTS
#define WINDOWFINDERDLG_API __declspec(dllexport)
#else
#define WINDOWFINDERDLG_API __declspec(dllimport)
#endif

/**
 * @param app_main_hwnd  App's main window's hwnd
 * @return the found window's hwnd
 */
extern "C"
{
    WINDOWFINDERDLG_API HWND WINAPI StartSearchWindowDlg(HWND parent);
}
