using Mono.Cecil;

namespace StationeersMods.Shared
{
    public static class CecilExtensions
    {
        /// <summary>
        ///     Is this Type a subclass of the other Type?
        /// </summary>
        /// <param name="self">A TypeDefinition.</param>
        /// <param name="other">A TypeDefinition.</param>
        /// <returns>True if this TypeDefinition is a subclass of the other TypeDefinition.</returns>
        public static bool IsSubClassOf(this TypeDefinition self, TypeName other)
        {
            return self.IsSubClassOf(other.nameSpace, other.name);
        }

        /// <summary>
        ///     Is this Type a subclass of the other Type?
        /// </summary>
        /// <param name="self">A TypeDefinition.</param>
        /// <param name="namespace">A Type's namespace.</param>
        /// <param name="name">A Type's name.</param>
        /// <returns>True if this TypeDefinition is a subclass of the Type.</returns>
        public static bool IsSubClassOf(this TypeDefinition self, string @namespace, string name)
        {
            var type = self;

            while (type != null)
            {
                if (type.BaseType != null)
                    try
                    {
                        type = type.BaseType.Resolve();
                    }
                    catch (AssemblyResolutionException e)
                    {
                        LogUtility.LogWarning("Could not resolve " + e.AssemblyReference.Name + " in IsSubClassOf().");
                        return false;
                    }
                else
                    type = null;

                if (type != null)
                    if (type.Namespace == @namespace && type.Name == name)
                        return true;
            }

            return false;
        }

        /// <summary>
        ///     Get the first method that matches with methodName.
        /// </summary>
        /// <param name="self">A TypeDefinition.</param>
        /// <param name="methodName">A method's name</param>
        /// <returns>The MethodDefinition for the method, if found. Null otherwise.</returns>
        public static MethodDefinition GetMethod(this TypeDefinition self, string methodName)
        {
            foreach (var method in self.Methods)
                if (method.Name == methodName)
                    return method;

            return null;
        }

        /// <summary>
        ///     Get the first field that matches with fieldName.
        /// </summary>
        /// <param name="self">A TypeDefinition.</param>
        /// <param name="fieldName">The FieldDefinition for the field, if found. Null otherwise.</param>
        /// <returns>The FieldDefinition, or null of none was found.</returns>
        public static FieldDefinition GetField(this TypeDefinition self, string fieldName)
        {
            foreach (var field in self.Fields)
                if (field.Name == fieldName)
                    return field;

            return null;
        }

        /// <summary>
        ///     Get the first property that matches with propertyName.
        /// </summary>
        /// <param name="self">A TypeDefinition.</param>
        /// <param name="propertyName">The PropertyDefinition for the field, if found. Null otherwise.</param>
        /// <returns>The PropertyDefinition, or null of none was found.</returns>
        public static PropertyDefinition GetProperty(this TypeDefinition self, string propertyName)
        {
            foreach (var property in self.Properties)
                if (property.Name == propertyName)
                    return property;

            return null;
        }
    }
}