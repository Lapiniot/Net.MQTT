﻿// <auto-generated />
using System.Runtime.CompilerServices;

#pragma warning disable 219, 612, 618
#nullable disable

namespace Mqtt.Server.Identity.Data.Compiled
{
    public static class IdentityRoleClaimUnsafeAccessors<TKey>
        where TKey : IEquatable<TKey>
    {
        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "<Id>k__BackingField")]
        public static extern ref int Id(IdentityRoleClaim<TKey> @this);

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "<ClaimType>k__BackingField")]
        public static extern ref string ClaimType(IdentityRoleClaim<TKey> @this);

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "<ClaimValue>k__BackingField")]
        public static extern ref string ClaimValue(IdentityRoleClaim<TKey> @this);

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "<RoleId>k__BackingField")]
        public static extern ref TKey RoleId(IdentityRoleClaim<TKey> @this);
    }
}