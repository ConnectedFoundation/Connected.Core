namespace Connected.Annotations.Entities;
/// <summary>
/// Specifies how a persistence is treated for the Entities and its properties.
/// </summary>
[Flags]
public enum PersistenceMode
{
	/// <summary>
	/// The Entity or property should be kept only in memory. No storage reads or writes will
	/// be performed.
	/// </summary>
	InMemory = 0,
	/// <summary>
	/// The Entity or property should perform only reads from storage but no writes.
	/// </summary>
	Read = 1,
	/// <summary>
	/// The Entity or property should perform only writes to the storage but will be ignored
	/// when reading.
	/// </summary>
	Write = 2,
	/// <summary>
	/// A default, a storage will perform read and write operations for the Entity or its property.
	/// </summary>
	ReadWrite = 3
}
/// <summary>
/// Specifies how entity or one ore more of its properties are persisted.
/// </summary>
/// <remarks>
/// Creates a new instance of the PersistenceAttribute class.
/// </remarks>
/// <param name="mode">Specifies how persistence will be treated by a storage provider.</param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class)]
public sealed class PersistenceAttribute(PersistenceMode mode)
		: Attribute
{
	/// <summary>
	/// Gets the persistence mode.
	/// </summary>
	public PersistenceMode Mode { get; } = mode;
	/// <summary>
	/// Gets the value which indicates wether the Entity or property is read only.
	/// </summary>
	public bool IsReadOnly => (Mode & PersistenceMode.Read) == PersistenceMode.Read;
	/// <summary>
	/// Gets the value which indicates wether the Entity or property is write only.
	/// </summary>
	public bool IsWriteOnly => (Mode & PersistenceMode.Write) == PersistenceMode.Write;
	/// <summary>
	/// Gets the value which indicates wether the Entity or property supports reading and writing.
	/// </summary>
	public bool IsReadWrite => (Mode & PersistenceMode.ReadWrite) == PersistenceMode.ReadWrite;
	/// <summary>
	/// Gets the value which indicates wether the Entity or property is present only in memory.
	/// </summary>
	public bool IsVirtual => Mode == PersistenceMode.InMemory;
}
