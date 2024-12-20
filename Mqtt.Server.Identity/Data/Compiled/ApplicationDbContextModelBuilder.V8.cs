﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

#pragma warning disable 219, 612, 618
#nullable disable

namespace Mqtt.Server.Identity.Data.Compiled
{
    public partial class ApplicationDbContextModel
    {
        private ApplicationDbContextModel()
            : base(skipDetectChanges: false, modelId: new Guid("7e567263-9ade-47b9-9c44-f22f2f1871d9"), entityTypeCount: 7)
        {
        }

        partial void Initialize()
        {
            var identityRole = IdentityRoleEntityType.Create(this);
            var identityRoleClaim = IdentityRoleClaimEntityType.Create(this);
            var identityUserClaim = IdentityUserClaimEntityType.Create(this);
            var identityUserLogin = IdentityUserLoginEntityType.Create(this);
            var identityUserRole = IdentityUserRoleEntityType.Create(this);
            var identityUserToken = IdentityUserTokenEntityType.Create(this);
            var applicationUser = ApplicationUserEntityType.Create(this);

            IdentityRoleClaimEntityType.CreateForeignKey1(identityRoleClaim, identityRole);
            IdentityUserClaimEntityType.CreateForeignKey1(identityUserClaim, applicationUser);
            IdentityUserLoginEntityType.CreateForeignKey1(identityUserLogin, applicationUser);
            IdentityUserRoleEntityType.CreateForeignKey1(identityUserRole, identityRole);
            IdentityUserRoleEntityType.CreateForeignKey2(identityUserRole, applicationUser);
            IdentityUserTokenEntityType.CreateForeignKey1(identityUserToken, applicationUser);

            IdentityRoleEntityType.CreateAnnotations(identityRole);
            IdentityRoleClaimEntityType.CreateAnnotations(identityRoleClaim);
            IdentityUserClaimEntityType.CreateAnnotations(identityUserClaim);
            IdentityUserLoginEntityType.CreateAnnotations(identityUserLogin);
            IdentityUserRoleEntityType.CreateAnnotations(identityUserRole);
            IdentityUserTokenEntityType.CreateAnnotations(identityUserToken);
            ApplicationUserEntityType.CreateAnnotations(applicationUser);

            AddAnnotation("ProductVersion", "9.0.0");
        }
    }
}