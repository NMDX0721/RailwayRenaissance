"""Read the Cubism accessibility tree through its bundled Java Access Bridge.

ABI definitions follow the OpenJDK 17 AccessBridgePackages.h interface.
The default mode only queries the editor. An explicit guarded invocation can
operate one previously inspected control through its advertised action.
"""
import ctypes
import argparse
from ctypes import wintypes
import json
import os
from pathlib import Path
import time

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'tools/vn/cubism_eye_rig_review'
JRE = Path('E:/Live2D_Cubism_5.3_beta/app/jre/bin')


class ContextInfo(ctypes.Structure):
    _fields_ = [
        ('name', ctypes.c_wchar * 1024), ('description', ctypes.c_wchar * 1024),
        ('role', ctypes.c_wchar * 256), ('role_en_US', ctypes.c_wchar * 256),
        ('states', ctypes.c_wchar * 256), ('states_en_US', ctypes.c_wchar * 256),
        ('indexInParent', ctypes.c_int), ('childrenCount', ctypes.c_int),
        ('x', ctypes.c_int), ('y', ctypes.c_int), ('width', ctypes.c_int), ('height', ctypes.c_int),
        ('accessibleComponent', wintypes.BOOL), ('accessibleAction', wintypes.BOOL),
        ('accessibleSelection', wintypes.BOOL), ('accessibleText', wintypes.BOOL),
        ('accessibleInterfaces', wintypes.BOOL),
    ]


class ActionInfo(ctypes.Structure):
    _fields_ = [('name', ctypes.c_wchar * 256)]


class AccessibleActions(ctypes.Structure):
    _fields_ = [('count', ctypes.c_int), ('items', ActionInfo * 256)]


class ActionsToDo(ctypes.Structure):
    _fields_ = [('count', ctypes.c_int), ('items', ActionInfo * 32)]


def editor_windows(user32):
    windows = []
    callback_type = ctypes.WINFUNCTYPE(wintypes.BOOL, wintypes.HWND, wintypes.LPARAM)

    @callback_type
    def collect(hwnd, _):
        length = user32.GetWindowTextLengthW(hwnd)
        text = ctypes.create_unicode_buffer(length + 1)
        user32.GetWindowTextW(hwnd, text, length + 1)
        process_id = wintypes.DWORD()
        user32.GetWindowThreadProcessId(hwnd, ctypes.byref(process_id))
        windows.append((hwnd, text.value, process_id.value))
        return True

    user32.EnumWindows(collect, 0)
    editor_processes = {pid for _, title, pid in windows if title.startswith('Live2D Cubism Editor')}
    return [(hwnd, title) for hwnd, title, pid in windows
            if pid in editor_processes and user32.IsWindowVisible(hwnd)]


def pump(user32, duration):
    deadline = time.monotonic() + duration
    message = wintypes.MSG()
    while time.monotonic() < deadline:
        while user32.PeekMessageW(ctypes.byref(message), None, 0, 0, 1):
            user32.TranslateMessage(ctypes.byref(message))
            user32.DispatchMessageW(ctypes.byref(message))
        time.sleep(.02)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--window', type=int)
    parser.add_argument('--start-path', type=json.loads)
    parser.add_argument('--invoke-path', type=json.loads)
    parser.add_argument('--expect-name')
    parser.add_argument('--watch-seconds', type=int, default=0)
    args = parser.parse_args()
    if args.start_path is not None:
        assert args.window and all(isinstance(i, int) and 0 <= i < 10000 for i in args.start_path)
    if args.invoke_path is not None:
        assert args.window and args.expect_name
        assert all(isinstance(i, int) and 0 <= i < 10000 for i in args.invoke_path)
    assert os.name == 'nt' and ctypes.sizeof(ctypes.c_void_p) == 8
    user32 = ctypes.WinDLL('user32', use_last_error=True)
    user32.GetWindowTextLengthW.argtypes = [wintypes.HWND]
    user32.GetWindowTextW.argtypes = [wintypes.HWND, wintypes.LPWSTR, ctypes.c_int]
    user32.GetWindowThreadProcessId.argtypes = [wintypes.HWND, ctypes.POINTER(wintypes.DWORD)]
    user32.IsWindowVisible.argtypes = [wintypes.HWND]
    user32.DispatchMessageW.argtypes = [ctypes.POINTER(wintypes.MSG)]
    user32.DispatchMessageW.restype = wintypes.LPARAM
    with os.add_dll_directory(str(JRE)):
        bridge = ctypes.WinDLL(str(JRE / 'windowsaccessbridge-64.dll'))
    bridge.Windows_run.argtypes = []
    bridge.Windows_run.restype = None
    bridge.isJavaWindow.argtypes = [wintypes.HWND]
    bridge.isJavaWindow.restype = wintypes.BOOL
    bridge.getAccessibleContextFromHWND.argtypes = [
        wintypes.HWND, ctypes.POINTER(ctypes.c_long), ctypes.POINTER(ctypes.c_int64),
    ]
    bridge.getAccessibleContextFromHWND.restype = wintypes.BOOL
    bridge.getAccessibleContextInfo.argtypes = [ctypes.c_long, ctypes.c_int64, ctypes.POINTER(ContextInfo)]
    bridge.getAccessibleContextInfo.restype = wintypes.BOOL
    bridge.getAccessibleChildFromContext.argtypes = [ctypes.c_long, ctypes.c_int64, ctypes.c_int]
    bridge.getAccessibleChildFromContext.restype = ctypes.c_int64
    bridge.releaseJavaObject.argtypes = [ctypes.c_long, ctypes.c_int64]
    bridge.releaseJavaObject.restype = None
    bridge.getAccessibleActions.argtypes = [ctypes.c_long, ctypes.c_int64, ctypes.POINTER(AccessibleActions)]
    bridge.getAccessibleActions.restype = wintypes.BOOL
    bridge.doAccessibleActions.argtypes = [ctypes.c_long, ctypes.c_int64,
                                         ctypes.POINTER(ActionsToDo), ctypes.POINTER(ctypes.c_int)]
    bridge.doAccessibleActions.restype = wintypes.BOOL
    bridge.Windows_run()
    pump(user32, 3)
    report = {'status': 'no_editor_window', 'readOnly': args.invoke_path is None, 'windows': [], 'nodes': []}

    def visit(vm, context, path):
        try:
            if len(report['nodes']) >= 1400 or len(path) > 36:
                return
            info = ContextInfo()
            if not bridge.getAccessibleContextInfo(vm, context, ctypes.byref(info)):
                return
            assert 0 <= info.childrenCount < 10000
            actions = AccessibleActions()
            available_actions = []
            if info.accessibleAction and bridge.getAccessibleActions(vm, context, ctypes.byref(actions)):
                if 0 <= actions.count <= 256:
                    available_actions = [actions.items[i].name for i in range(actions.count)]
            report['nodes'].append({
                'window': hwnd, 'path': path, 'name': info.name, 'role': info.role_en_US,
                'states': info.states_en_US, 'children': info.childrenCount,
                'actionable': bool(info.accessibleAction),
                'availableActions': available_actions,
                'bounds': [info.x, info.y, info.width, info.height],
            })
            for index in range(min(info.childrenCount, 120)):
                child = bridge.getAccessibleChildFromContext(vm, context, index)
                if child:
                    visit(vm, child, [*path, index])
        finally:
            bridge.releaseJavaObject(vm, context)

    deadline = time.monotonic() + min(max(args.watch_seconds, 0), 180)
    while True:
        report['windows'] = []
        report['nodes'] = []
        for hwnd, title in editor_windows(user32):
            if args.window and hwnd != args.window:
                continue
            is_java = bool(bridge.isJavaWindow(hwnd))
            record = {'hwnd': hwnd, 'title': title, 'javaAccessible': is_java}
            report['windows'].append(record)
            report['status'] = 'bridge_not_connected'
            if not is_java:
                continue
            vm = ctypes.c_long()
            context = ctypes.c_int64()
            if bridge.getAccessibleContextFromHWND(hwnd, ctypes.byref(vm), ctypes.byref(context)):
                current = context.value
                path = []
                for index in args.start_path or []:
                    child = bridge.getAccessibleChildFromContext(vm.value, current, index)
                    bridge.releaseJavaObject(vm.value, current)
                    current = child
                    path.append(index)
                    if not current:
                        break
                if current:
                    visit(vm.value, current, path)
                report['status'] = 'tree_read' if report['nodes'] else 'context_without_nodes'
        if args.watch_seconds:
            OUT.mkdir(exist_ok=True)
            (OUT / 'jab_watch.json').write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
            print(json.dumps({'watchWindows': report['windows'], 'nodes': len(report['nodes'])}, ensure_ascii=False), flush=True)
        if time.monotonic() >= deadline:
            break
        pump(user32, 1)
    if args.invoke_path is not None:
        observed = [n for n in report['nodes']
                    if n['window'] == args.window and n['path'] == args.invoke_path]
        assert len(observed) == 1 and observed[0]['name'] == args.expect_name
        assert 'enabled' in observed[0]['states'].split(',')
        assert observed[0]['availableActions'] == ['单击']
        vm = ctypes.c_long()
        context = ctypes.c_int64()
        assert bridge.getAccessibleContextFromHWND(args.window, ctypes.byref(vm), ctypes.byref(context))
        current = context.value
        try:
            for index in args.invoke_path:
                child = bridge.getAccessibleChildFromContext(vm.value, current, index)
                bridge.releaseJavaObject(vm.value, current)
                current = 0
                assert child
                current = child
            info = ContextInfo()
            assert bridge.getAccessibleContextInfo(vm.value, current, ctypes.byref(info))
            assert info.name == args.expect_name
            todo = ActionsToDo()
            todo.count = 1
            todo.items[0].name = '单击'
            failure = ctypes.c_int(-1)
            print(f'JAB_INVOKING {args.expect_name}', flush=True)
            result = bool(bridge.doAccessibleActions(vm.value, current, ctypes.byref(todo), ctypes.byref(failure)))
            report['action'] = {'name': args.expect_name, 'success': result, 'failureIndex': failure.value}
            assert result, report['action']
        finally:
            if current:
                bridge.releaseJavaObject(vm.value, current)
    OUT.mkdir(exist_ok=True)
    (OUT / 'jab_probe.json').write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
    print(json.dumps({**report, 'nodes': len(report['nodes'])}, ensure_ascii=False), flush=True)


if __name__ == '__main__':
    main()
