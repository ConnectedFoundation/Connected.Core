namespace Connected.Annotations.Entities;
/// <summary>
/// Base attribute for specifying Entity schema.
/// </summary>
/// <remarks>
/// If Entity implementation supports permanent storage it must set one of the
/// atributes that inherits from this attribute.
/// </remarks>
public abstract class SchemaAttribute : MappingAttribute
{
	/// <summary>
	/// A default storage schema. This value should be avoided since
	/// is can cause overlapping in the Entity names in the storage level.
	/// </summary>
	public const string DefaultSchema = "dbo";
	/// <summary>
	/// Defines a Schema table type.
	/// </summary>
	public const string SchemaTypeTable = "Table";
	/// <summary>
	/// A schema name that is being used by Core Entities.
	/// </summary>
	public const string CoreSchema = "core";
	/// <summary>
	/// A schema name that is being used by common Core Entities.
	/// </summary>
	public const string CommonSchema = "common";
	/// <summary>
	/// A schema name that is being used by marketing domain Entities.
	/// </summary>
	public const string MarketingSchema = "marketing";
	/// <summary>
	/// A schema name that is being used by planning domain Entities.
	/// </summary>
	public const string PlanningSchema = "planning";
	/// <summary>
	/// A schema name that is being used by customers domain Entities.
	/// </summary>
	public const string CustomersSchema = "customers";
	/// <summary>
	/// A schema name that is being used by documents domain Entities.
	/// </summary>
	public const string DocumentsSchema = "documents";
	/// <summary>
	/// A schema name that is being used by supply domain Entities.
	/// </summary>
	public const string SupplySchema = "supply";
	/// <summary>
	/// A schema name that is being used by manufacturing domain Entities.
	/// </summary>
	public const string ManufacturingSchema = "manufacturing";
	/// <summary>
	/// A schema name that is being used by logistics domain Entities.
	/// </summary>
	public const string LogisticsSchema = "logistics";
	/// <summary>
	/// A schema name that is being used by sales domain Entities.
	/// </summary>
	public const string SalesSchema = "sales";
	/// <summary>
	/// A schema name that is being used by maintenance domain Entities.
	/// </summary>
	public const string MaintenanceSchema = "maintenance";
	/// <summary>
	/// A schema name that is being used by projects domain Entities.
	/// </summary>
	public const string ProjectsSchema = "projects";
	/// <summary>
	/// A schema name that is being used by quality domain Entities.
	/// </summary>
	public const string QualitySchema = "quality";
	/// <summary>
	/// A schema name that is being used by resources domain Entities.
	/// </summary>
	public const string ResourcesSchema = "resources";
	/// <summary>
	/// A schema name that is being used by assets domain Entities.
	/// </summary>
	public const string AssetsSchema = "assets";
	/// <summary>
	/// A schema name that is being used by accounting domain Entities.
	/// </summary>
	public const string AccountingSchema = "accounting";
	/// <summary>
	/// A schema name that is being used by knowledge domain Entities.
	/// </summary>
	public const string KnowledgeSchema = "knowledge";
	/// <summary>
	/// Gets or sets the Id of the Schema. Id is typically being used as a unique
	/// identifier by Storage Providers when synchronizing schema.
	/// </summary>
	public string? Id { get; set; }
	/// <summary>
	/// Gets or sets the Name of the Entity storage object. This property typically equals to the storage
	/// object name. If this value is ommited, Storage Providers typically use type name
	/// as a replacement.
	/// </summary>
	public string? Name { get; set; }
	/// <summary>
	/// Gets or sets the name of the Schema that will be used when creating a storage object. If this
	/// value is not set the Exception should be thrown by the Schema middleware.
	/// </summary>
	public string? Schema { get; set; } = DefaultSchema;
}