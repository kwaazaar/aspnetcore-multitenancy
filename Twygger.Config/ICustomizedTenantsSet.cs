namespace Kwaazaar.Config
{
    internal interface ICustomizedTenantsSet
    {
        void AddTenantId(string tenantId);
        bool ContainsTenantId(string tenantId);
    }
}