﻿using Microsoft.VisualStudio.TestTools.UnitTesting;

[assembly: CLSCompliant(false)]
[assembly: Parallelize(Workers = 4, Scope = ExecutionScope.MethodLevel)]
