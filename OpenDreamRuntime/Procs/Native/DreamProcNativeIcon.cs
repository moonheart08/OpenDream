using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Objects.MetaObjects;
using OpenDreamRuntime.Resources;

namespace OpenDreamRuntime.Procs.Native {
    static class DreamProcNativeIcon {
        [DreamProc("Width")]
        public static DreamValue NativeProc_Width(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            DreamIcon dreamIconObject = DreamMetaObjectIcon.ObjectToDreamIcon[instance];

            return new DreamValue(dreamIconObject.Width);
        }

        [DreamProc("Height")]
        public static DreamValue NativeProc_Height(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            DreamIcon dreamIconObject = DreamMetaObjectIcon.ObjectToDreamIcon[instance];

            return new DreamValue(dreamIconObject.Height);
        }

        [DreamProc("Insert")]
        [DreamProcParameter("new_icon", Type = DreamValue.DreamValueType.DreamObject)]
        [DreamProcParameter("icon_state", Type = DreamValue.DreamValueType.String)]
        [DreamProcParameter("dir", Type = DreamValue.DreamValueType.Float)]
        [DreamProcParameter("frame", Type = DreamValue.DreamValueType.Float)]
        [DreamProcParameter("moving", Type = DreamValue.DreamValueType.Float)]
        [DreamProcParameter("delay", Type = DreamValue.DreamValueType.Float)]
        public static DreamValue NativeProc_Insert(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            //TODO Figure out what happens when you pass the wrong types as args

            DreamValue newIcon = arguments.GetArgument(0, "new_icon");
            DreamValue iconState = arguments.GetArgument(1, "icon_state");
            DreamValue dir = arguments.GetArgument(2, "dir");
            DreamValue frame = arguments.GetArgument(3, "frame");
            DreamValue moving = arguments.GetArgument(4, "moving");
            DreamValue delay = arguments.GetArgument(5, "delay");

            // TODO: moving & delay

            var resourceManager = IoCManager.Resolve<DreamResourceManager>();
            var (iconRsc, iconDescription) = DreamMetaObjectIcon.GetIconResourceAndDescription(resourceManager, newIcon);

            DreamIcon iconObj = DreamMetaObjectIcon.ObjectToDreamIcon[instance];
            iconObj.InsertStates(iconRsc, iconDescription, iconState, dir, frame); // TODO: moving & delay
            return DreamValue.Null;
        }

        [DreamProc("Blend")]
        [DreamProcParameter("icon", Type = DreamValue.DreamValueType.DreamObject)]
        [DreamProcParameter("function", Type = DreamValue.DreamValueType.Float)]
        [DreamProcParameter("x", Type = DreamValue.DreamValueType.Float)]
        [DreamProcParameter("y", Type = DreamValue.DreamValueType.Float)]
        public static DreamValue NativeProc_Blend(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            //TODO Figure out what happens when you pass the wrong types as args

            DreamValue icon = arguments.GetArgument(0, "icon");
            DreamValue function = arguments.GetArgument(1, "function");

            arguments.GetArgument(2, "x").TryGetValueAsInteger(out var x);
            arguments.GetArgument(3, "y").TryGetValueAsInteger(out var y);

            if (!function.TryGetValueAsInteger(out var functionValue))
                throw new Exception($"Invalid 'function' argument {function}");

            var blendType = (DreamIconOperationBlend.BlendType) functionValue;

            DreamIcon iconObj = DreamMetaObjectIcon.ObjectToDreamIcon[instance];
            iconObj.ApplyOperation(new DreamIconOperationBlend(blendType, icon, x, y));
            return DreamValue.Null;
        }

        [DreamProc("Scale")]
        [DreamProcParameter("width", Type = DreamValue.DreamValueType.Float)]
        [DreamProcParameter("height", Type = DreamValue.DreamValueType.Float)]
        public static DreamValue NativeProc_Scale(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            //TODO Figure out what happens when you pass the wrong types as args

            arguments.GetArgument(0, "width").TryGetValueAsInteger(out var width);
            arguments.GetArgument(1, "height").TryGetValueAsInteger(out var height);

            DreamIcon iconObj = DreamMetaObjectIcon.ObjectToDreamIcon[instance];
            iconObj.Width = width;
            iconObj.Height = height;
            return DreamValue.Null;
        }
    }
}
