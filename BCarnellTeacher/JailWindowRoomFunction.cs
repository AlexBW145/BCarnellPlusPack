using System;
using System.Reflection;

namespace BCarnellTeacher;

public class JailWindowRoomFunction : RoomFunction
{
    public override void OnGenerationFinished()
    {
        base.OnGenerationFinished();
        room.ec.offices.Remove(room);
        foreach (var door in room.doors)
        {
            IntVector2 pos = door.position + Direction.West.ToIntVector2().Adjusted(default, door.direction.GetOpposite());
            Cell cell;
            if (room.ec.ContainsCoordinates(pos + door.direction.ToIntVector2()))
            {
                cell = room.ec.CellFromPosition(pos);
                if (!cell.Null && !room.ec.CellFromPosition(pos + door.direction.ToIntVector2()).WallHardCovered(door.direction.GetOpposite()))
                    room.ec.BuildWindow(cell, door.direction, room.windowObject);
            }
            pos = door.position + Direction.East.ToIntVector2().Adjusted(default, door.direction.GetOpposite());
            if (room.ec.ContainsCoordinates(pos + door.direction.ToIntVector2()))
            {
                cell = room.ec.CellFromPosition(pos);
                if (!cell.Null && !room.ec.CellFromPosition(pos + door.direction.ToIntVector2()).WallHardCovered(door.direction.GetOpposite()))
                    room.ec.BuildWindow(cell, door.direction, room.windowObject);
            }
        }
    }
}
