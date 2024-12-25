namespace Connected.Storage.Schemas;

public interface ITable : ISchema
{
	List<ITableColumn> TableColumns { get; }
	List<ITableIndex> Indexes { get; }
}
