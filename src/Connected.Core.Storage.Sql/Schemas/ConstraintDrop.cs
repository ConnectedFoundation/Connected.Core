using System.Text;

namespace Connected.Storage.Sql.Schemas;

internal class ConstraintDrop : TableTransaction
{
	public ConstraintDrop(ObjectIndex index)
	{
		Index = index;
	}

	private ObjectIndex Index { get; }

	protected override async Task OnExecute()
	{
		await Context.Execute(CommandText);
	}
	private string CommandText
	{
		get
		{
			var text = new StringBuilder();

			text.AppendLine($"ALTER TABLE {Escape(Context.Schema.Schema, Context.Schema.Name)} DROP CONSTRAINT {Index.Name};");

			return text.ToString();
		}
	}
}
