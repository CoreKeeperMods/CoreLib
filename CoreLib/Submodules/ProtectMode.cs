﻿namespace CoreLib.Submodules;

public enum ProtectMode
{
    PAGE_NOACCESS = 0x1,
    PAGE_READONLY = 0x2,
    PAGE_READWRITE  = 0x4,
    PAGE_WRITECOPY = 0x8,
    PAGE_EXECUTE = 0x10,
    PAGE_EXECUTE_READ = 0x20,
    PAGE_EXECUTE_READWRITE = 0x40,
    PAGE_EXECUTE_WRITECOPY = 0x80
}