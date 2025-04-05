using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpaceshipGenerator : MonoBehaviour
{
    [System.Serializable]
    public class RoomType
    {
        public string name;
        public GameObject prefab;
        public Vector3 minSize = new Vector3(5, 3, 5);
        public Vector3 maxSize = new Vector3(15, 5, 15);
        public bool isRequired = false;
        public int maxCount = 1;
        public List<string> requiredConnections = new List<string>();
        public List<string> optionalConnections = new List<string>();
    }
    
    [System.Serializable]
    public class RoomData
    {
        public string roomType;
        public Vector3 position;
        public Vector3 dimensions;
        public Quaternion rotation = Quaternion.identity;
        public List<DoorData> doors = new List<DoorData>();
        
        public RoomData(string type, Vector3 pos, Vector3 size)
        {
            roomType = type;
            position = pos;
            dimensions = size;
        }
        
        public Bounds GetBounds()
        {
            return new Bounds(position, dimensions);
        }
    }
    
    [System.Serializable]
    public class DoorData
    {
        public Vector3 position;
        public Quaternion rotation;
        public string connectedRoomId;
        public bool isConnected = false;
        
        public DoorData(Vector3 pos, Quaternion rot)
        {
            position = pos;
            rotation = rot;
        }
    }

    [Header("Room Configuration")]
    public List<RoomType> roomTypes = new List<RoomType>();
    public float doorwayWidth = 2.0f;
    public float doorwayHeight = 2.5f;
    public GameObject doorPrefab;
    public GameObject corridorPrefab;

    [Header("Generation Settings")]
    public int minRooms = 5;
    public int maxRooms = 15;
    public float corridorWidth = 2.0f;
    public Vector2 shipSize = new Vector2(100f, 100f);
    public bool useCorridors = true;
    public float roomSpacing = 2.0f;
    
    [Header("Debug")]
    public bool showDebugVisuals = true;
    
    [Header("Save/Load")]
    public string layoutName = "SpaceshipLayout";
    public bool saveAfterGeneration = false;
    
    // Internal tracking
    private List<Room> generatedRooms = new List<Room>();
    private Dictionary<string, List<Room>> roomsByType = new Dictionary<string, List<Room>>();
    private List<Connection> connections = new List<Connection>();
    
    // Layout data for saving/loading
    [System.Serializable]
    public class SpaceshipLayout
    {
        public string layoutName;
        public List<RoomData> rooms = new List<RoomData>();
        public List<ConnectionData> connections = new List<ConnectionData>();
        public Vector2 shipDimensions;
        
        [System.Serializable]
        public class ConnectionData
        {
            public string roomAId;
            public string roomBId;
            public int doorAIndex;
            public int doorBIndex;
        }
    }
    
    private SpaceshipLayout currentLayout = new SpaceshipLayout();

    public class Room
    {
        public string id;
        public string type;
        public GameObject instance;
        public RoomData data;
        public List<Connection> connections = new List<Connection>();
        
        public Room(string type, Vector3 position, Vector3 size, GameObject instance)
        {
            this.id = System.Guid.NewGuid().ToString();
            this.type = type;
            this.instance = instance;
            this.data = new RoomData(type, position, size);
        }
        
        public Bounds GetBounds()
        {
            return data.GetBounds();
        }
        
        public Vector3 GetPosition()
        {
            return data.position;
        }
        
        public Vector3 GetSize()
        {
            return data.dimensions;
        }
        
        public void GeneratePossibleDoorPositions(float doorwayWidth, float doorwayHeight)
        {
            // Generate door positions on each wall
            float halfWidth = data.dimensions.x / 2;
            float halfLength = data.dimensions.z / 2;
            float doorOffsetY = doorwayHeight / 2;
            
            // For better alignment, ensure doors are placed at fixed positions
            // that align with the grid
            
            // X-axis walls (North and South) - doors in the center of each wall
            // North wall - facing positive Z
            Vector3 northPos = new Vector3(
                data.position.x,
                data.position.y + doorOffsetY,
                data.position.z + halfLength
            );
            data.doors.Add(new DoorData(northPos, Quaternion.Euler(0, 0, 0)));
            
            // South wall - facing negative Z
            Vector3 southPos = new Vector3(
                data.position.x,
                data.position.y + doorOffsetY,
                data.position.z - halfLength
            );
            data.doors.Add(new DoorData(southPos, Quaternion.Euler(0, 180, 0)));
            
            // Z-axis walls (East and West) - doors in the center of each wall
            // East wall - facing positive X
            Vector3 eastPos = new Vector3(
                data.position.x + halfWidth,
                data.position.y + doorOffsetY,
                data.position.z
            );
            data.doors.Add(new DoorData(eastPos, Quaternion.Euler(0, 90, 0)));
            
            // West wall - facing negative X
            Vector3 westPos = new Vector3(
                data.position.x - halfWidth,
                data.position.y + doorOffsetY,
                data.position.z
            );
            data.doors.Add(new DoorData(westPos, Quaternion.Euler(0, 270, 0)));
        }
    }
    
    public class Connection
    {
        public string id;
        public Room roomA;
        public Room roomB;
        public DoorData doorA;
        public DoorData doorB;
        public GameObject corridor;
        
        public Connection(Room a, Room b, DoorData dA, DoorData dB)
        {
            id = System.Guid.NewGuid().ToString();
            roomA = a;
            roomB = b;
            doorA = dA;
            doorB = dB;
            
            // Mark doors as connected and reference each other
            doorA.isConnected = true;
            doorB.isConnected = true;
            doorA.connectedRoomId = roomB.id;
            doorB.connectedRoomId = roomA.id;
        }
        
        public Vector3 GetDoorPositionA()
        {
            return doorA.position;
        }
        
        public Vector3 GetDoorPositionB()
        {
            return doorB.position;
        }
    }

    public void GenerateSpaceship()
    {
        CleanupPreviousGeneration();
        InitializeRoomTypeDictionary();
        GenerateRequiredRooms();
        GenerateOptionalRooms();
        ConnectRooms();
        CreateCorridorsAndDoors();
        FinalizeSpaceship();
        
        if (saveAfterGeneration)
        {
            SaveLayout(layoutName);
        }
    }
    
    public void SaveLayout(string name)
    {
        currentLayout = new SpaceshipLayout();
        currentLayout.layoutName = name;
        currentLayout.shipDimensions = shipSize;
        
        // Save rooms
        foreach (Room room in generatedRooms)
        {
            currentLayout.rooms.Add(room.data);
        }
        
        // Save connections
        foreach (Connection conn in connections)
        {
            SpaceshipLayout.ConnectionData connData = new SpaceshipLayout.ConnectionData();
            connData.roomAId = conn.roomA.id;
            connData.roomBId = conn.roomB.id;
            connData.doorAIndex = conn.roomA.data.doors.IndexOf(conn.doorA);
            connData.doorBIndex = conn.roomB.data.doors.IndexOf(conn.doorB);
            
            currentLayout.connections.Add(connData);
        }
        
#if UNITY_EDITOR
        // Save to asset
        string path = "Assets/SpaceshipLayouts/";
        if (!System.IO.Directory.Exists(path))
        {
            System.IO.Directory.CreateDirectory(path);
        }
        
        string filePath = path + name + ".json";
        string json = JsonUtility.ToJson(currentLayout, true);
        System.IO.File.WriteAllText(filePath, json);
        
        UnityEditor.AssetDatabase.Refresh();
        Debug.Log("Saved layout to " + filePath);
#endif
    }
    
    public void LoadLayout(string name)
    {
#if UNITY_EDITOR
        string filePath = "Assets/SpaceshipLayouts/" + name + ".json";
        if (System.IO.File.Exists(filePath))
        {
            string json = System.IO.File.ReadAllText(filePath);
            SpaceshipLayout layout = JsonUtility.FromJson<SpaceshipLayout>(json);
            
            InstantiateLayout(layout);
        }
        else
        {
            Debug.LogError("Layout file not found: " + filePath);
        }
#endif
    }
    
    private void InstantiateLayout(SpaceshipLayout layout)
    {
        CleanupPreviousGeneration();
        
        // Set ship size from layout
        shipSize = layout.shipDimensions;
        
        // First instantiate all rooms
        Dictionary<string, Room> roomsById = new Dictionary<string, Room>();
        
        foreach (RoomData roomData in layout.rooms)
        {
            // Find room type definition
            RoomType roomType = roomTypes.Find(rt => rt.name == roomData.roomType);
            if (roomType == null)
            {
                Debug.LogError("Room type not found: " + roomData.roomType);
                continue;
            }
            
            // Instantiate room
            GameObject roomObj = Instantiate(roomType.prefab, roomData.position, roomData.rotation, transform);
            roomObj.name = roomData.roomType + "_" + generatedRooms.Count;
            roomObj.transform.localScale = roomData.dimensions;
            
            // Create room instance
            Room room = new Room(roomData.roomType, roomData.position, roomData.dimensions, roomObj);
            room.id = System.Guid.NewGuid().ToString(); // Generate new ID
            room.data = roomData; // Use loaded data
            
            // Add to collections
            generatedRooms.Add(room);
            if (!roomsByType.ContainsKey(roomData.roomType))
            {
                roomsByType[roomData.roomType] = new List<Room>();
            }
            roomsByType[roomData.roomType].Add(room);
            roomsById[room.id] = room;
        }
        
        // Then create connections
        foreach (SpaceshipLayout.ConnectionData connData in layout.connections)
        {
            if (!roomsById.ContainsKey(connData.roomAId) || !roomsById.ContainsKey(connData.roomBId))
            {
                Debug.LogError("Room not found for connection");
                continue;
            }
            
            Room roomA = roomsById[connData.roomAId];
            Room roomB = roomsById[connData.roomBId];
            
            if (connData.doorAIndex >= roomA.data.doors.Count || connData.doorBIndex >= roomB.data.doors.Count)
            {
                Debug.LogError("Door index out of range");
                continue;
            }
            
            DoorData doorA = roomA.data.doors[connData.doorAIndex];
            DoorData doorB = roomB.data.doors[connData.doorBIndex];
            
            // Create connection
            Connection connection = new Connection(roomA, roomB, doorA, doorB);
            connections.Add(connection);
            roomA.connections.Add(connection);
            roomB.connections.Add(connection);
        }
        
        // Create physical corridors and doors
        CreateCorridorsAndDoors();
        
        Debug.Log("Loaded layout with " + generatedRooms.Count + " rooms and " + connections.Count + " connections");
    }
    
    public void CleanupPreviousGeneration()
    {
        // Destroy all previously generated objects
        foreach (Room room in generatedRooms)
        {
            if (room.instance != null)
                DestroyImmediate(room.instance);
        }
        
        // Explicitly find and destroy all corridors
        GameObject[] existingCorridors = GameObject.FindGameObjectsWithTag("Corridor");
        foreach (GameObject corridor in existingCorridors)
        {
            DestroyImmediate(corridor);
        }
        
        // Alternative cleanup that doesn't rely on tags
        foreach (Transform child in transform)
        {
            if (child.name.Contains("Corridor"))
            {
                DestroyImmediate(child.gameObject);
            }
        }
        
        foreach (Connection connection in connections)
        {
            if (connection.corridor != null)
                DestroyImmediate(connection.corridor);
        }
        
        generatedRooms.Clear();
        roomsByType.Clear();
        connections.Clear();
    }
    
    private void InitializeRoomTypeDictionary()
    {
        // Initialize dictionary for quick lookups
        foreach (RoomType type in roomTypes)
        {
            roomsByType[type.name] = new List<Room>();
        }
    }
    
    private void GenerateRequiredRooms()
    {
        // First, place all required rooms
        foreach (RoomType type in roomTypes)
        {
            if (type.isRequired)
            {
                int count = Mathf.Min(type.maxCount, 1); // At least one if required
                for (int i = 0; i < count; i++)
                {
                    PlaceRoom(type);
                }
            }
        }
    }
    
    private void GenerateOptionalRooms()
    {
        // Then add optional rooms up to min count
        int totalRooms = generatedRooms.Count;
        int roomsToAdd = Random.Range(minRooms, maxRooms + 1) - totalRooms;
        
        for (int i = 0; i < roomsToAdd; i++)
        {
            // Select a random room type that hasn't reached its max count
            List<RoomType> availableTypes = roomTypes.FindAll(
                type => !type.isRequired || roomsByType[type.name].Count < type.maxCount
            );
            
            if (availableTypes.Count == 0)
                break;
                
            RoomType selectedType = availableTypes[Random.Range(0, availableTypes.Count)];
            PlaceRoom(selectedType);
        }
    }
    
    private Room PlaceRoom(RoomType type)
    {
        // Generate random size within constraints
        Vector3 size = new Vector3(
            Random.Range(type.minSize.x, type.maxSize.x),
            Random.Range(type.minSize.y, type.maxSize.y),
            Random.Range(type.minSize.z, type.maxSize.z)
        );
        
        // Try to find a valid position
        Vector3 position = FindValidRoomPosition(size);
        
        // Instantiate room - ensure it's at the correct position and with proper scale
        GameObject roomObj = Instantiate(type.prefab, position, Quaternion.identity, transform);
        roomObj.name = $"{type.name}_{roomsByType[type.name].Count}";
        
        // Make sure the object's transform is set correctly
        roomObj.transform.position = position;
        roomObj.transform.localScale = size;
        
        Debug.Log($"Placed room {type.name} at position {position} with size {size}");
        
        // Create room data
        Room newRoom = new Room(type.name, position, size, roomObj);
        
        // Generate possible door positions
        GeneratePossibleDoorPositions(newRoom);
        
        // Add to collections
        generatedRooms.Add(newRoom);
        roomsByType[type.name].Add(newRoom);
        
        return newRoom;
    }
    
    private Vector3 FindValidRoomPosition(Vector3 size)
    {
        // Keep rooms on a grid to improve alignment
        float gridSize = 5.0f; // Match this to your level architecture
        
        // Try to find a valid position that doesn't overlap
        int maxAttempts = 100;
        for (int i = 0; i < maxAttempts; i++)
        {
            // Generate position on a grid
            float x = Mathf.Round(Random.Range(-shipSize.x/2 + size.x/2, shipSize.x/2 - size.x/2) / gridSize) * gridSize;
            float z = Mathf.Round(Random.Range(-shipSize.y/2 + size.z/2, shipSize.y/2 - size.z/2) / gridSize) * gridSize;
            
            Vector3 testPos = new Vector3(x, 0, z); // Rooms at same Y level
            
            Bounds testBounds = new Bounds(testPos, size + new Vector3(roomSpacing, 0, roomSpacing));
            
            if (!OverlapsAnyRoom(testBounds))
            {
                return testPos;
            }
            // No else here - we want to continue the loop and try more positions
        }
        
        // If we couldn't find a spot after all attempts, use a fallback position
        Debug.LogWarning("Couldn't find non-overlapping position for room");
        
        // Place it on the edge of the ship with grid snapping
        float fallbackX = Mathf.Round((-shipSize.x/2 + size.x/2 + generatedRooms.Count * 10) / gridSize) * gridSize;
        float fallbackZ = Mathf.Round((-shipSize.y/2 + size.z/2) / gridSize) * gridSize;
        
        return new Vector3(fallbackX, 0, fallbackZ);
    }
    
    private bool OverlapsAnyRoom(Bounds testBounds)
    {
        foreach (Room room in generatedRooms)
        {
            Bounds roomBounds = room.GetBounds();
            // Expand bounds by spacing
            roomBounds.Expand(new Vector3(roomSpacing, 0, roomSpacing));
            
            if (roomBounds.Intersects(testBounds))
            {
                return true;
            }
        }
        return false;
    }
    
    private void GeneratePossibleDoorPositions(Room room)
    {
        room.GeneratePossibleDoorPositions(doorwayWidth, doorwayHeight);
    }
    
    private void ConnectRooms()
    {
        // Create a MST (Minimum Spanning Tree) to ensure all rooms are connected
        List<Room> connectedRooms = new List<Room>();
        List<Room> unconnectedRooms = new List<Room>(generatedRooms);
        
        Debug.Log($"Starting ConnectRooms with {unconnectedRooms.Count} rooms to connect");
        
        if (unconnectedRooms.Count == 0)
            return;
            
        // Start with a random room
        Room startRoom = unconnectedRooms[Random.Range(0, unconnectedRooms.Count)];
        connectedRooms.Add(startRoom);
        unconnectedRooms.Remove(startRoom);
        
        Debug.Log($"Starting with room {startRoom.type}");
        
        // Connect all rooms
        while (unconnectedRooms.Count > 0)
        {
            float closestDistance = float.MaxValue;
            Room closestUnconnected = null;
            Room closestConnected = null;
            DoorData bestDoorA = null;
            DoorData bestDoorB = null;
            
            // Find the closest pair of rooms
            foreach (Room connectedRoom in connectedRooms)
            {
                foreach (Room unconnectedRoom in unconnectedRooms)
                {
                    // Find the best door positions between these rooms
                    DoorData doorA, doorB;
                    float distance = FindBestDoorPositions(connectedRoom, unconnectedRoom, out doorA, out doorB);
                    
                    if (distance < closestDistance && doorA != null && doorB != null)
                    {
                        closestDistance = distance;
                        closestConnected = connectedRoom;
                        closestUnconnected = unconnectedRoom;
                        bestDoorA = doorA;
                        bestDoorB = doorB;
                    }
                }
            }
            
            if (closestUnconnected != null && bestDoorA != null && bestDoorB != null)
            {
                Debug.Log($"Connecting {closestConnected.type} to {closestUnconnected.type} with distance {closestDistance}");
                
                // Create connection
                Connection connection = new Connection(
                    closestConnected, 
                    closestUnconnected,
                    bestDoorA,
                    bestDoorB
                );
                
                connections.Add(connection);
                closestConnected.connections.Add(connection);
                closestUnconnected.connections.Add(connection);
                
                // Move room to connected set
                connectedRooms.Add(closestUnconnected);
                unconnectedRooms.Remove(closestUnconnected);
            }
            else
            {
                Debug.LogError("Failed to connect all rooms");
                break;
            }
        }
        
        Debug.Log($"Created {connections.Count} primary connections");
        
        // Add some additional connections for loops (optional) - but much fewer
        // Instead of 20%, use a smaller percentage or fixed small number
        int additionalConnections = Mathf.Min(2, Mathf.FloorToInt(generatedRooms.Count * 0.1f)); // 10% more connections, max 2
        Debug.Log($"Attempting to add {additionalConnections} additional connections");
        
        int addedConnections = 0;
        
        for (int i = 0; i < additionalConnections * 3; i++) // Try more attempts to find good connections
        {
            // Pick two random rooms
            Room roomA = generatedRooms[Random.Range(0, generatedRooms.Count)];
            Room roomB = generatedRooms[Random.Range(0, generatedRooms.Count)];
            
            // Don't connect a room to itself or already connected rooms
            if (roomA == roomB || AreRoomsConnected(roomA, roomB))
                continue;
                
            // Find door positions
            DoorData doorA, doorB;
            float distance = FindBestDoorPositions(roomA, roomB, out doorA, out doorB);
            
            // Create connection if distance is reasonable and doors are available
            // Use a stricter distance threshold to avoid very long corridors
            if (distance < 20f && doorA != null && doorB != null) // More strict threshold
            {
                Debug.Log($"Adding additional connection from {roomA.type} to {roomB.type} with distance {distance}");
                
                Connection connection = new Connection(roomA, roomB, doorA, doorB);
                connections.Add(connection);
                roomA.connections.Add(connection);
                roomB.connections.Add(connection);
                
                addedConnections++;
                if (addedConnections >= additionalConnections)
                    break;
            }
        }
        
        Debug.Log($"Added {addedConnections} additional connections for a total of {connections.Count} connections");
    }
    
    private bool AreRoomsConnected(Room a, Room b)
    {
        foreach (Connection conn in connections)
        {
            if ((conn.roomA == a && conn.roomB == b) || (conn.roomA == b && conn.roomB == a))
                return true;
        }
        return false;
    }
    
    private float FindBestDoorPositions(Room roomA, Room roomB, out DoorData doorA, out DoorData doorB)
    {
        float shortestDistance = float.MaxValue;
        doorA = null;
        doorB = null;
        
        int availableDoorsA = 0;
        int availableDoorsB = 0;
        
        foreach (DoorData dA in roomA.data.doors)
        {
            // Skip doors that are already connected
            if (dA.isConnected)
                continue;
                
            availableDoorsA++;
            
            foreach (DoorData dB in roomB.data.doors)
            {
                // Skip doors that are already connected
                if (dB.isConnected)
                    continue;
                    
                availableDoorsB++;
                
                float distance = Vector3.Distance(dA.position, dB.position);
                if (distance < shortestDistance)
                {
                    shortestDistance = distance;
                    doorA = dA;
                    doorB = dB;
                }
            }
        }
        
        Debug.Log($"Room {roomA.type} has {availableDoorsA} available doors, Room {roomB.type} has {availableDoorsB} available doors");
        
        if (doorA == null || doorB == null)
        {
            Debug.LogWarning($"Could not find available doors between rooms {roomA.type} and {roomB.type}");
            return float.MaxValue;
        }
        
        Debug.Log($"Best door pair has distance {shortestDistance} between {roomA.type} and {roomB.type}");
        return shortestDistance;
    }
    
    private void CreateCorridorsAndDoors()
    {
        Debug.Log($"Creating corridors and doors for {connections.Count} connections");
        Debug.Log($"useCorridors is set to: {useCorridors}");
        
        foreach (Connection connection in connections)
        {
            // Calculate corridor direction
            Vector3 directionA = connection.GetDoorPositionB() - connection.GetDoorPositionA();
            float distance = directionA.magnitude;
            
            Debug.Log($"Connection distance: {distance}, threshold: {doorwayWidth * 1.5f}");
            
            // Determine orientation
            bool isHorizontal = Mathf.Abs(directionA.x) > Mathf.Abs(directionA.z);
            
            if (useCorridors && distance > doorwayWidth * 1.5f)
            {
                Debug.Log("Creating corridor for connection");
                // Create corridor using an L-shape if rooms aren't aligned
                CreateCorridor(connection);
            }
            else
            {
                Debug.Log("Creating only doors (no corridor) for connection");
                // Just create doors
                CreateDoor(connection.doorA, connection.roomA.instance.transform);
                CreateDoor(connection.doorB, connection.roomB.instance.transform);
            }
        }
    }
    
    private void CreateCorridor(Connection connection)
    {
        Vector3 start = connection.GetDoorPositionA();
        Vector3 end = connection.GetDoorPositionB();
        
        Debug.Log($"Creating corridor from {start} to {end}");
        
        // Calculate the distance between doors
        float distance = Vector3.Distance(start, end);
        
        // If doors are very close, just create the doors, no corridor needed
        if (distance < 3.0f)
        {
            CreateDoor(connection.doorA, connection.roomA.instance.transform);
            CreateDoor(connection.doorB, connection.roomB.instance.transform);
            return;
        }
        
        // For simplicity, create direct corridors rather than L-shaped ones
        // Single corridor segment going directly from door to door
        Vector3 direction = end - start;
        
        // Normalize the direction and create a direct corridor
        direction.Normalize();
        
        // Calculate the center point between doors for corridor placement
        Vector3 centerPoint = (start + end) / 2.0f;
        
        // Create a single corridor segment
        GameObject segment = CreateCorridorSegment(centerPoint, direction);
        if (segment != null)
        {
            connection.corridor = segment;
        }
        
        // Create doors at the endpoints
        CreateDoor(connection.doorA, connection.roomA.instance.transform);
        CreateDoor(connection.doorB, connection.roomB.instance.transform);
    }
    
    private GameObject CreateCorridorSegment(Vector3 position, Vector3 direction)
    {
        Debug.Log($"Creating corridor segment at position {position} with direction {direction}");
        
        if (corridorPrefab == null)
        {
            Debug.LogError("corridorPrefab is null! Please assign a corridor prefab in the inspector.");
            return null;
        }
        
        // Create the corridor with correct rotation
        Quaternion rotation = Quaternion.LookRotation(direction);
        
        // Instantiate the corridor at the given position with the calculated rotation
        GameObject corridor = Instantiate(corridorPrefab, position, rotation, transform);
        corridor.name = "Corridor";
        
        // Tag the corridor for easier cleanup
        corridor.tag = "Corridor";
        
        Debug.Log($"Created corridor at {position} with rotation {rotation.eulerAngles}");
        return corridor;
    }
    
    private void CreateDoor(DoorData doorData, Transform parentRoom)
    {
        if (doorPrefab != null)
        {
            Debug.Log($"Creating door at position {doorData.position} with rotation {doorData.rotation.eulerAngles}");
            
            // Create the door at the exact position and rotation
            GameObject door = Instantiate(doorPrefab, doorData.position, doorData.rotation);
            door.name = "Door";
            
            // Explicitly set position and rotation again to ensure accuracy
            door.transform.position = doorData.position;
            door.transform.rotation = doorData.rotation;
            
            // Make the door a child of the parent room
            door.transform.SetParent(parentRoom, true);
        }
        else
        {
            Debug.LogWarning("Door prefab is not assigned! Cannot create door.");
        }
    }
    
    private void FinalizeSpaceship()
    {
        if (showDebugVisuals)
        {
            // Draw debug lines for connections
            StartCoroutine(DrawDebugLines());
        }
        
        Debug.Log($"Generated spaceship with {generatedRooms.Count} rooms and {connections.Count} connections");
    }
    
    private IEnumerator DrawDebugLines()
    {
        // This will keep debug visuals active in play mode
        while (showDebugVisuals)
        {
            foreach (Connection connection in connections)
            {
                Debug.DrawLine(connection.GetDoorPositionA(), connection.GetDoorPositionB(), Color.yellow);
            }
            yield return null;
        }
    }
    
    // Helper function for the editor
    private void OnDrawGizmos()
    {
        if (!showDebugVisuals)
            return;
            
        // Draw ship bounds
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(shipSize.x, 5, shipSize.y));
        
        // Draw room bounds
        foreach (Room room in generatedRooms)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(room.GetPosition(), room.GetSize());
            
            // Draw door positions
            Gizmos.color = Color.red;
            foreach (DoorData door in room.data.doors)
            {
                Gizmos.DrawSphere(door.position, 0.3f);
                // Draw small arrow to indicate door direction
                Gizmos.DrawRay(door.position, door.rotation * Vector3.forward * 0.5f);
            }
        }
    }
}