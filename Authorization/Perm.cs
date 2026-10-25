using System.Reflection;

namespace RbacApi.Authorization
{
    public static class Perm
    {
        // user permissions
        public const string USER_READ = "user.read";
        public const string USER_CREATE = "user.create";
        public const string USER_UPDATE = "user.update";
        public const string USER_DELETE = "user.delete";

        // role permissions
        public const string ROLE_READ = "role.read";
        public const string ROLE_CREATE = "role.create";
        public const string ROLE_UPDATE = "role.update";
        public const string ROLE_DELETE = "role.delete";
        // assign permission
        public const string ROLE_ASSIGN_PERMISSION = "role.assign_permission";


        // permission permissions
        public const string PERMISSION_READ = "permission.read";
        public const string PERMISSION_CREATE = "permission.create";
        public const string PERMISSION_UPDATE = "permission.update";
        public const string PERMISSION_DELETE = "permission.delete";

        

        public static IReadOnlyList<string> GetAll()
        {
            return typeof(Perm)
                .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(fi => fi.IsLiteral && !fi.IsInitOnly && fi.FieldType == typeof(string))
                .Select(fi => (string)fi.GetValue(null)!)
                .ToList();
        }
    }
}
