﻿using System.Collections.Generic;

namespace Lithnet.AccessManager.Agent
{
    public interface IJitSettings
    {
        bool CreateJitGroup { get; }

        string JitGroupCreationOU { get; }

        int JitGroupType { get; }

        string JitGroup { get; }

        string JitGroupDescription { get; }

        IEnumerable<string> AllowedAdmins { get; }

        bool RestrictAdmins { get; }
        
        bool JitEnabled { get; }
    }
}