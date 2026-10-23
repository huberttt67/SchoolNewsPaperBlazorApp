﻿namespace SchoolNewspaperBlazorApp.Data
{
    public class Permission
    {
        public int Id { get; set; }

        public EnumPermission Name { get; set; }

        public string Description { get; set; } = string.Empty;

        public ICollection<UserPermission> UserPermissions { get; set; }
            = new List<UserPermission>();
        public string PermissionName { get; internal set; }
    }
}