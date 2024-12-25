namespace Connected.Authorization;

public enum EmptyPolicyBehavior
{
	Deny = 1,
	Alow = 2
}

public enum AuthorizationStrategy
{
	Pessimistic = 1,
	Optimistic = 2
}

public interface IAuthorizationSchema
{
	EmptyPolicyBehavior EmptyPolicy { get; set; }
	AuthorizationStrategy Strategy { get; set; }
}
