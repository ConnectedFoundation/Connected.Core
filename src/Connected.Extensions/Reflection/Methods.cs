using Connected.Services;
using System.Reflection;

namespace Connected.Reflection;

public static class Methods
{
	/// <summary>
	/// Resolves method based on generic arguments and parameter types.
	/// </summary>
	/// <param name="type">The type on which method is declared.</param>
	/// <param name="name">The name of the method.</param>
	/// <param name="typeArguments">The type arguments if a method is a generic method definition.</param>
	/// <param name="parameterTypes">The argument types which method accepts.</param>
	/// <returns></returns>
	public static MethodInfo? ResolveMethod(this Type type, string name, Type[]? typeArguments, Type[]? parameterTypes)
	{
		var typeArgumentCount = typeArguments is not null ? typeArguments.Length : 0;
		/*
		 * First, get all methods available on the type.
		 */
		foreach (var method in type.GetInheritedMethods())
		{
			/*
			 * If a method is a generic method and type arguments weren't passed,
			 * skip this method because we are not interested in it.
			 */
			if (method.IsGenericMethodDefinition != typeArgumentCount > 0)
				continue;
			/*
			 * Also, the name of the method must match, of course.
			 */
			if (!string.Equals(method.Name, name))
				continue;
			/*
			 * Now check if the type arguments match.
			 */
			if (method.IsGenericMethodDefinition && typeArguments is not null && typeArguments.Any())
			{
				/*
				 * Check if the number of arguments equals on both the definition and typeArguments argument.
				 */
				if (method.GetGenericArguments().Length != typeArgumentCount)
					continue;
				/*
				 * Now try to create a generic method definition.
				 */
				var constructedMethod = method.MakeGenericMethod(typeArguments);
				/*
				 * And if parameters also match we have a target.
				 */
				if (ParametersMatch(constructedMethod.GetParameters(), parameterTypes))
					return constructedMethod;
			}
			/*
			 * The method is not a generic method, we're only going to check the parameters types.
			 */
			if (ParametersMatch(method.GetParameters(), parameterTypes))
				return method;
		}

		return default;
	}

	internal static bool ParametersMatch(ParameterInfo[] parameters, Type[]? parameterTypes)
	{
		/*
		 * Parameterless service methods must pass IDto argument into api methods. First check this.
		 */
		if (parameterTypes is not null && parameterTypes.Length == 1 && parameterTypes[0] == typeof(IDto) && (parameters is null || parameters.Length == 0))
			return true;

		if (parameterTypes is null)
			return parameters.Length == 0;

		if (parameters.Length != parameterTypes.Length)
			return false;

		for (var i = 0; i < parameters.Length; i++)
		{
			if (!parameters[i].ParameterType.IsAssignableFrom(parameterTypes[i]))
				return false;
		}

		return true;
	}

	public static IEnumerable<MethodInfo> GetInheritedMethods(this Type type)
	{
		foreach (var info in type.GetInheritedTypeInfos())
		{
			foreach (var p in info.GetRuntimeMethods())
				yield return p;
		}
	}

	public static async Task<object?> InvokeAsync(this MethodInfo method, object component, params object?[] parameters)
	{
		if (method.ReturnType is null)
		{
			method.Invoke(component, parameters);

			return await Task.FromResult<object?>(null);
		}
		else
		{
			var isAwaitable = method.ReturnType.GetMethod(nameof(Task.GetAwaiter)) is not null;

			if (isAwaitable)
			{
				if (method.ReturnType.IsGenericType)
				{
					var returnValue = method.Invoke(component, parameters);

					if (returnValue is null)
						return null;

					return await (dynamic)returnValue;
				}
				else
				{
					var returnValue = method.Invoke(component, parameters);

					if (returnValue is null)
						return null;

					await ((Task)returnValue).ConfigureAwait(true);
					return null;
				}
			}
			else
			{
				if (method.ReturnType == typeof(void))
					method.Invoke(component, parameters);
				else
					return method.Invoke(component, parameters);
			}
		}

		return null;
	}
}
