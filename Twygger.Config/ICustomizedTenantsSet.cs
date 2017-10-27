namespace Twygger.Config
{
    public interface ICustomizedTenantsSet
    {
        void AddTenantId(string tenantId);
        bool ContainsTenantId(string tenantId);
    }
}