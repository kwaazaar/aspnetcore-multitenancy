namespace Kwaazaar.Config
{
    /// <summary>
    /// Base-class for configuration models
    /// </summary>
    public abstract class ConfigModel
    {
        /// <summary>
        /// Applies TenantId to this model. A possible implementation could be to replace a tenantid-variable with the actual tenantid.
        /// Has no default implementation.
        /// </summary>
        /// <param name="tenantId">The active tenant, could be empty string</param>
        public virtual void SetTenantId(string tenantId) { }

        /// <summary>
        /// Validate the model after retrieval
        /// </summary>
        public virtual void Validate() { }
    }
}
