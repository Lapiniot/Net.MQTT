﻿// <auto-generated />
using System.Runtime.CompilerServices;

#pragma warning disable 219, 612, 618
#nullable disable

namespace Mqtt.Server.Identity.Data.Compiled
{
    public static class IdentityUserRoleUnsafeAccessors<TKey>
        where TKey : IEquatable<TKey>
    {
        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "<UserId>k__BackingField")]
        public static extern ref TKey UserId(IdentityUserRole<TKey> @this);

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "<RoleId>k__BackingField")]
        public static extern ref TKey RoleId(IdentityUserRole<TKey> @this);
    }
}