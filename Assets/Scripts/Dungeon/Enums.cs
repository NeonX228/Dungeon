using Sirenix.OdinInspector;

namespace Dungeon
{
    public static class Enums
    {
        /// <summary>
        /// Specifies the mode of dungeon generation.
        /// </summary>
        public enum GenerationMode
        {
            /// <summary>
            /// Represents the mode of dungeon generation where the entire process is completed immediately
            /// without yielding or waiting. This mode is suitable for scenarios where the generation speed
            /// and immediate availability of the dungeon layout are prioritized over progressive generation.
            /// </summary>
            [LabelText(SdfIconType.LightningChargeFill)] Instant,

            /// <summary>
            /// Represents a generation mode where dungeon generation occurs incrementally over multiple frames.
            /// This mode is designed to divide the dungeon creation process into smaller tasks utilizing coroutines,
            /// which can help prevent the application from becoming unresponsive during complex operations.
            /// </summary>
            [LabelText(SdfIconType.MoonFill)] Coroutine
        }

        /// <summary>
        /// Represents the method of splitting regions in a dungeon generation process.
        /// </summary>
        public enum SplitMethod
        {
            /// <summary>
            /// Specifies the vertical splitting method for dividing a room in the dungeon generation process.
            /// </summary>
            /// <remarks>
            /// When this method is used, the room is divided along the vertical axis into two smaller rooms.
            /// The resulting subrooms will have equal or configured bounds as determined by the generation logic.
            /// This splitting method is commonly used to alternate with horizontal splitting for recursive room
            /// division in procedural dungeon generation.
            /// </remarks>
            Verticaly,

            /// <summary>
            /// Represents the horizontal split method for partitioning a room or dungeon area.
            /// This method divides a room or area along its horizontal axis, effectively splitting
            /// it into a top and bottom section. Used primarily in procedural dungeon generation
            /// to create structured layouts.
            /// </summary>
            Horizontaly,
        }

        /// <summary>
        /// Specifies the direction of a door in a dungeon generation system.
        /// </summary>
        public enum DoorDirection
        {
            /// <summary>
            /// Represents a door direction along the X-axis within the dungeon.
            /// </summary>
            X,

            /// <summary>
            /// Represents a door direction that is aligned along the Z-axis.
            /// Used to specify the orientation of a door in relation to the
            /// Z-axis within the dungeon generation system.
            /// </summary>
            Z,

            /// <summary>
            /// Represents the absence of a door direction.
            /// </summary>
            /// <remarks>
            /// The <c>None</c> value is used to indicate that a given wall or boundary does not have a door or is not designated
            /// for door placement. This is typically used as a default or fallback state in the context of the dungeon generation logic.
            /// </remarks>
            None
        }

        /// <summary>
        /// Defines the types of wall prefabs used in the dungeon generation system.
        /// </summary>
        public enum WallPrefabType
        {
            /// <summary>
            /// Represents a wall prefab type designed for a '+' shaped intersection in the dungeon layout.
            /// This type is used when four connecting pathways meet at a single point, forming
            /// a crossroad-like structure.
            /// </summary>
            IntersectionPlus,

            /// <summary>
            /// Represents a T-shaped wall intersection in the dungeon layout.
            /// This type of wall prefab is typically used to create a junction where three wall segments meet,
            /// forming a shape resembling the letter "T".
            /// </summary>
            IntersectionT,

            /// <summary>
            /// Represents a corner wall type prefab used in the dungeon generation process.
            /// This enum member is utilized for defining corner-shaped wall tiles
            /// within the procedural dungeon creation system.
            /// </summary>
            Corner,

            /// <summary>
            /// Represents a wall prefab type used for creating long continuous wall segments in the dungeon generation process.
            /// </summary>
            LongWall,

            /// <summary>
            /// Represents a short wall type used in dungeon generation.
            /// </summary>
            /// <remarks>
            /// This enum member is commonly used to designate prefabs for creating shorter wall sections
            /// within the dungeon layout. It is part of the WallPrefabType enumeration utilized in the dungeon
            /// generator to map specific wall configurations.
            /// </remarks>
            ShortWall,

            /// <summary>
            /// Represents a pillar wall prefab type in the dungeon generation system.
            /// This type is used for placing vertical, standalone structural elements
            /// to enhance the architectural realism and provide support-like visual effects
            /// in generated dungeons.
            /// </summary>
            Pillar
        }
    }
}
