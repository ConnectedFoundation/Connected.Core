﻿namespace Connected.Annotations.Entities;
/// <summary>
/// Defines a mapping on the Entity and / or its Properties. This is just a markup attribute
/// which is specialized by other Schema related attributes.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field)]
public abstract class MappingAttribute : Attribute
{
}
