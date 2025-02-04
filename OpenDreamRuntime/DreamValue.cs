﻿using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using JetBrains.Annotations;
using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Objects.MetaObjects;
using OpenDreamRuntime.Resources;
using OpenDreamShared.Dream;
using OpenDreamRuntime.Procs;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace OpenDreamRuntime {
    [JsonConverter(typeof(DreamValueJsonConverter))]
    public struct DreamValue : IEquatable<DreamValue> {
        [Flags]
        public enum DreamValueType {
            String = 1,
            Float = 2,
            DreamResource = 4,
            DreamObject = 8,
            DreamPath = 16,
            DreamProc = 32,
            Reference = 64
        }

        public static readonly DreamValue Null = new DreamValue((DreamObject?)null);
        public static DreamValue True { get => new DreamValue(1f); }
        public static DreamValue False { get => new DreamValue(0f); }

        public DreamValueType Type { get; private set; }
        public object Value { get; private set; }

        public DreamValue(String value) {
            Type = DreamValueType.String;
            Value = value;
        }

        public DreamValue(float value) {
            Type = DreamValueType.Float;
            Value = value;
        }

        public DreamValue(int value) : this((float)value) { }

        public DreamValue(UInt32 value) : this((float)value) { }

        public DreamValue(double value) : this((float)value) { }

        public DreamValue(DreamResource value) {
            Type = DreamValueType.DreamResource;
            Value = value;
        }

        /// <remarks> This constructor is also how one creates nulls. </remarks>
        public DreamValue(DreamObject? value) {
            Type = DreamValueType.DreamObject;
            Value = value;
        }

        public DreamValue(DreamPath value) {
            Type = DreamValueType.DreamPath;
            Value = value;
        }

        public DreamValue(DreamProc value) {
            Type = DreamValueType.DreamProc;
            Value = value;
        }

        public DreamValue(object value) {
            if (value is int) {
                Value = (float)(int)value;
            } else {
                Value = value;
            }

            Type = value switch {
                string => DreamValueType.String,
                int => DreamValueType.Float,
                float => DreamValueType.Float,
                DreamResource => DreamValueType.DreamResource,
                DreamObject => DreamValueType.DreamObject,
                DreamPath => DreamValueType.DreamPath,
                DreamProc => DreamValueType.DreamProc,
                DreamProcArguments => DreamValueType.Reference,
                _ => throw new ArgumentException("Invalid DreamValue value (" + value + ", " + value.GetType() + ")")
            };
        }

        public override string ToString() {
            string strValue;
            if (Value == null) {
                strValue = "null";
            } else if (Type == DreamValueType.String) {
                strValue = $"\"{Value}\"";
            } else {
                strValue = Value.ToString() ?? "<ToString() = null>";
            }

            return "DreamValue(" + Type + ", " + strValue + ")";
        }

        [Obsolete("Deprecated. Use the relevant TryGetValueAs[Type]() instead.")]
        private object GetValueExpectingType(DreamValueType type) {
            if (Type == type) {
                return Value;
            }

            throw new Exception("Value " + this + " was not the expected type of " + type + "");
        }

        [Obsolete("Deprecated. Use TryGetValueAsString() instead.")]
        public string GetValueAsString() {
            return (string)GetValueExpectingType(DreamValueType.String);
        }

        public bool TryGetValueAsString([NotNullWhen(true)] out string? value) {
            if (Type == DreamValueType.String) {
                value = (string)Value;
                return true;
            } else {
                value = null;
                return false;
            }
        }

        //Casts a float value to an integer
        [Obsolete("Deprecated. Use TryGetValueAsInteger() instead.")]
        public int GetValueAsInteger() {
            return (int)(float)GetValueExpectingType(DreamValueType.Float);
        }

        public bool TryGetValueAsInteger(out int value) {
            if (Type == DreamValueType.Float) {
                value = (int)(float)Value;
                return true;
            } else {
                value = 0;
                return false;
            }
        }

        [Obsolete("Deprecated. Use TryGetValueAsFloat() instead.")]
        public float GetValueAsFloat() {
            return (float)GetValueExpectingType(DreamValueType.Float);
        }

        public bool TryGetValueAsFloat(out float value) {
            if (Type == DreamValueType.Float) {
                value = (float)Value;
                return true;
            } else {
                value = 0;
                return false;
            }
        }

        [Obsolete("Deprecated. Use TryGetValueAsDreamResource() instead.")]
        public DreamResource GetValueAsDreamResource() {
            return (DreamResource)GetValueExpectingType(DreamValueType.DreamResource);
        }

        public bool TryGetValueAsDreamResource([NotNullWhen(true)] out DreamResource? value) {
            if (Type == DreamValueType.DreamResource) {
                value = (DreamResource)Value;
                return true;
            } else {
                value = null;
                return false;
            }
        }

        [Obsolete("Deprecated. Use TryGetValueAsDreamObject() instead.")]
        public DreamObject? GetValueAsDreamObject() {
            DreamObject dreamObject = (DreamObject)GetValueExpectingType(DreamValueType.DreamObject);

            if (dreamObject?.Deleted == true) {
                Value = null;

                return null;
            } else {
                return dreamObject;
            }
        }

        public bool TryGetValueAsDreamObject(out DreamObject? dreamObject) {
            if (Type == DreamValueType.DreamObject) {
                dreamObject = GetValueAsDreamObject();
                return true;
            } else {
                dreamObject = null;
                return false;
            }
        }

        public bool TryGetValueAsDreamObjectOfType(DreamPath type, [NotNullWhen(true)] out DreamObject? dreamObject) {
            return TryGetValueAsDreamObject(out dreamObject) && dreamObject != null && dreamObject.IsSubtypeOf(type);
        }

        [Obsolete("Deprecated. Prefer TryGetValueAsDreamList(), or use MustGetValueAsDreamList() if you are sure.")]
        public DreamList GetValueAsDreamList() => MustGetValueAsDreamList();

        public DreamList MustGetValueAsDreamList() {
            if (TryGetValueAsDreamList(out var list))
                return list;
            throw new InvalidOperationException($"Expected a /list, but got {this}");
        }

        public bool TryGetValueAsDreamList([NotNullWhen(true)] out DreamList list) {
            if (TryGetValueAsDreamObjectOfType(DreamPath.List, out DreamObject listObject)) {
                list = (DreamList)listObject;

                return true;
            } else {
                list = null;

                return false;
            }
        }

        [Obsolete("Deprecated. Use TryGetValueAsPath() instead.")]
        public DreamPath GetValueAsPath() {
            return (DreamPath)GetValueExpectingType(DreamValueType.DreamPath);
        }

        public bool TryGetValueAsPath(out DreamPath path) {
            if (Type == DreamValueType.DreamPath) {
                path = (DreamPath)Value;

                return true;
            } else {
                path = DreamPath.Root;

                return false;
            }
        }

        public bool TryGetValueAsProc(out DreamProc proc) {
            if (Type == DreamValueType.DreamProc) {
                proc = (DreamProc)Value;

                return true;
            } else {
                proc = null;

                return false;
            }
        }

        public bool IsTruthy() {
            switch (Type) {
                case DreamValue.DreamValueType.DreamObject:
                    return Value != null && ((DreamObject)Value).Deleted == false;
                case DreamValue.DreamValueType.DreamProc:
                    return Value != null;
                case DreamValue.DreamValueType.DreamResource:
                case DreamValue.DreamValueType.DreamPath:
                    return true;
                case DreamValue.DreamValueType.Float:
                    return (float)Value != 0;
                case DreamValue.DreamValueType.String:
                    return (string)Value != "";
                default:
                    throw new NotImplementedException("Truthy evaluation for " + this + " is not implemented");
            }
        }

        public string Stringify() {
            switch (Type) {
                case DreamValueType.String:
                    TryGetValueAsString(out var stringString);
                    return stringString;
                case DreamValueType.Float:
                    TryGetValueAsFloat(out var floatString);
                    return floatString.ToString();
                case DreamValueType.DreamResource:
                    TryGetValueAsDreamResource(out var rscPath);
                    return rscPath.ResourcePath;
                case DreamValueType.DreamPath:
                    TryGetValueAsPath(out var path);
                    return path.PathString;
                case DreamValueType.DreamObject: {
                    if (TryGetValueAsDreamObject(out var dreamObject) && dreamObject != null) {
                        return dreamObject.GetDisplayName();
                    }

                    return String.Empty;
                }
                default:
                    throw new NotImplementedException("Cannot stringify " + this);
            }
        }

        public override bool Equals(object obj) => obj is DreamValue other && Equals(other);

        public bool Equals(DreamValue other) {
            if (Type != other.Type) return false;
            if (Value == null) return other.Value == null;
            return Value.Equals(other.Value);
        }

        public override int GetHashCode() {
            if (Value != null) {
                return Value.GetHashCode();
            }
            return 0;
        }

        public static bool operator ==(DreamValue a, DreamValue b) {
            return a.Equals(b);
        }

        public static bool operator !=(DreamValue a, DreamValue b) {
            return !a.Equals(b);
        }
    }

    #region Serialization
    public sealed class DreamValueJsonConverter : JsonConverter<DreamValue> {
        public override void Write(Utf8JsonWriter writer, DreamValue value, JsonSerializerOptions options) {
            writer.WriteStartObject();
            writer.WriteNumber("Type", (int)value.Type);

            switch (value.Type) {
                case DreamValue.DreamValueType.String: writer.WriteString("Value", (string)value.Value); break;
                case DreamValue.DreamValueType.Float: writer.WriteNumber("Value", (float)value.Value); break;
                case DreamValue.DreamValueType.DreamObject when value == DreamValue.Null: writer.WriteNull("Value"); break;
                case DreamValue.DreamValueType.DreamObject
                    when value.TryGetValueAsDreamObjectOfType(DreamPath.Icon, out var iconObj):
                {
                    // TODO Check what happens with multiple states
                    var icon = DreamMetaObjectIcon.ObjectToDreamIcon[iconObj];
                    var (resource, _) = icon.GenerateDMI();
                    var base64 = Convert.ToBase64String(resource.ResourceData);
                    writer.WriteString("Value", base64);
                    break;
                }
                default: throw new NotImplementedException("Json serialization for " + value + " is not implemented");
            }

            writer.WriteEndObject();
        }

        public override DreamValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType != JsonTokenType.StartObject) throw new Exception("Expected StartObject token");
            reader.Read();

            if (reader.GetString() != "Type") throw new Exception("Expected type property");
            reader.Read();
            DreamValue.DreamValueType type = (DreamValue.DreamValueType)reader.GetInt32();
            reader.Read();

            if (reader.GetString() != "Value") throw new Exception("Expected value property");
            reader.Read();

            DreamValue value;
            switch (type) {
                case DreamValue.DreamValueType.String: value = new DreamValue(reader.GetString()); break;
                case DreamValue.DreamValueType.Float: value = new DreamValue((float)reader.GetSingle()); break;
                case DreamValue.DreamValueType.DreamObject when reader.TokenType == JsonTokenType.Null: {
                    if (reader.TokenType == JsonTokenType.Null) {
                        value = DreamValue.Null;
                    } else {
                        throw new NotImplementedException("Json deserialization for DreamObjects are not implemented");
                    }

                    break;
                }
                default: throw new NotImplementedException("Json deserialization for type " + type + " is not implemented");
            }
            reader.Read();

            if (reader.TokenType != JsonTokenType.EndObject) throw new Exception("Expected EndObject token");

            return value;
        }
    }

    // The following allows for serializing using DreamValues with ISerializationManager
    // Currently only implemented to the point that they can be used for DreamFilters

    public sealed class DreamValueDataNode : DataNode<DreamValueDataNode>, IEquatable<DreamValueDataNode> {
        public DreamValueDataNode(DreamValue value) : base(NodeMark.Invalid, NodeMark.Invalid) {
            Value = value;
        }

        public DreamValue Value { get; set; }
        public override bool IsEmpty => false;

        public override DreamValueDataNode Copy() {
            return new DreamValueDataNode(Value) {Tag = Tag, Start = Start, End = End};
        }

        public override DreamValueDataNode? Except(DreamValueDataNode node) {
            return Value == node.Value ? null : Copy();
        }

        public override DreamValueDataNode PushInheritance(DreamValueDataNode node) {
            return Copy();
        }

        public bool Equals(DreamValueDataNode? other) {
            return Value == other?.Value;
        }
    }

    [TypeSerializer]
    public sealed class DreamValueStringSerializer : ITypeReader<string, DreamValueDataNode> {
        public string Read(ISerializationManager serializationManager, DreamValueDataNode node,
            IDependencyCollection dependencies,
            bool skipHook,
            ISerializationContext? context = null, string? value = default) {
            if (!node.Value.TryGetValueAsString(out var strValue))
                throw new Exception($"Value {node.Value} was not a string");

            return strValue;
        }

        public ValidationNode Validate(ISerializationManager serializationManager, DreamValueDataNode node,
            IDependencyCollection dependencies,
            ISerializationContext? context = null) {
            if (node.Value.TryGetValueAsString(out _))
                return new ValidatedValueNode(node);

            return new ErrorNode(node, $"Value {node.Value} is not a string");
        }
    }

    [TypeSerializer]
    public sealed class DreamValueFloatSerializer : ITypeReader<float, DreamValueDataNode> {
        public float Read(ISerializationManager serializationManager, DreamValueDataNode node,
            IDependencyCollection dependencies,
            bool skipHook,
            ISerializationContext? context = null, float value = default) {
            if (!node.Value.TryGetValueAsFloat(out var floatValue))
                throw new Exception($"Value {node.Value} was not a float");

            return floatValue;
        }

        public ValidationNode Validate(ISerializationManager serializationManager, DreamValueDataNode node,
            IDependencyCollection dependencies,
            ISerializationContext? context = null) {
            if (node.Value.TryGetValueAsFloat(out _))
                return new ValidatedValueNode(node);

            return new ErrorNode(node, $"Value {node.Value} is not a float");
        }
    }

    [TypeSerializer]
    public sealed class DreamValueColorSerializer : ITypeReader<Color, DreamValueDataNode> {
        public Color Read(ISerializationManager serializationManager, DreamValueDataNode node,
            IDependencyCollection dependencies,
            bool skipHook,
            ISerializationContext? context = null, Color value = default) {
            if (!node.Value.TryGetValueAsString(out var strValue) || !ColorHelpers.TryParseColor(strValue, out var color))
                throw new Exception($"Value {node.Value} was not a color");

            return color;
        }

        public ValidationNode Validate(ISerializationManager serializationManager, DreamValueDataNode node,
            IDependencyCollection dependencies,
            ISerializationContext? context = null) {
            if (node.Value.TryGetValueAsString(out var strValue) && ColorHelpers.TryParseColor(strValue, out _))
                return new ValidatedValueNode(node);

            return new ErrorNode(node, $"Value {node.Value} is not a color");
        }
    }

    [TypeSerializer]
    public sealed class DreamValueMatrix3Serializer : ITypeReader<Matrix3, DreamValueDataNode> {
        public Matrix3 Read(ISerializationManager serializationManager, DreamValueDataNode node,
            IDependencyCollection dependencies,
            bool skipHook,
            ISerializationContext? context = null, Matrix3 value = default) {
            if (!node.Value.TryGetValueAsDreamObjectOfType(DreamPath.Matrix, out var matrixObject))
                throw new Exception($"Value {node.Value} was not a matrix");

            // Matrix3 except not really because DM matrix is actually 3x2
            matrixObject.GetVariable("a").TryGetValueAsFloat(out var a);
            matrixObject.GetVariable("b").TryGetValueAsFloat(out var b);
            matrixObject.GetVariable("c").TryGetValueAsFloat(out var c);
            matrixObject.GetVariable("d").TryGetValueAsFloat(out var d);
            matrixObject.GetVariable("e").TryGetValueAsFloat(out var e);
            matrixObject.GetVariable("f").TryGetValueAsFloat(out var f);
            return new Matrix3(a, d, 0f, b, e, 0f, c, f, 1f);
        }

        public ValidationNode Validate(ISerializationManager serializationManager, DreamValueDataNode node,
            IDependencyCollection dependencies,
            ISerializationContext? context = null) {
            if (node.Value.TryGetValueAsDreamObjectOfType(DreamPath.Matrix, out _))
                return new ValidatedValueNode(node);

            return new ErrorNode(node, $"Value {node.Value} is not a matrix");
        }
    }
    #endregion Serialization
}
