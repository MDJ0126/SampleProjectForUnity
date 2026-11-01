using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Game.Editor")]

/// <summary>
/// 물리 조회의 디버그 선을 수명 동안 유지하고 게임 카메라에 메시로 표시한다.
/// </summary>
[DefaultExecutionOrder(32000)]
[AddComponentMenu("")]
internal sealed class PhysicsQueryDebugRenderer : MonoBehaviour
{
    private static PhysicsQueryDebugRenderer _instance;
    private LineBatch _overlay;
    private LineBatch _depthTest;
    private LineBatch _overlayMarkers;
    private LineBatch _depthMarkers;
    /// <summary>
    /// 만료된 선을 제거하고 이번 프레임의 선 메시를 갱신한다.
    /// </summary>
    private void LateUpdate()
    {
        _overlay?.UpdateMesh();
        _depthTest?.UpdateMesh();
        _overlayMarkers?.UpdateMesh();
        _depthMarkers?.UpdateMesh();
    }
    /// <summary>
    /// 갱신이 중단되어도 마지막 프레임의 선 메시가 계속 표시되지 않도록 비운다.
    /// </summary>
    private void OnDisable()
    {
        _overlay?.Clear();
        _depthTest?.Clear();
        _overlayMarkers?.Clear();
        _depthMarkers?.Clear();
    }
    /// <summary>
    /// 플레이 종료 시 생성한 메시와 머티리얼을 정리한다.
    /// </summary>
    private void OnDestroy()
    {
        DisposeResources(false);
        if (_instance == this)
        {
            _instance = null;
        }
    }

    /// <summary>
    /// 플레이 중 선분을 등록하고 필요한 렌더링 자원을 최초 호출에 생성한다.
    /// </summary>
    internal static void DrawLine(Vector3 start, Vector3 end, Color color, float duration, bool depthTest)
    {
        if (!EnsureInstance())
        {
            return;
        }
        LineBatch batch = depthTest ? _instance._depthTest : _instance._overlay;
        batch.Add(start, end, color, duration);
    }
    /// <summary>
    /// 감지 지점을 면이 채워진 정육면체 메시로 표시한다.
    /// </summary>
    internal static void DrawCube(Vector3 center, float size, Color color, float duration, bool depthTest)
    {
        if (!EnsureInstance())
        {
            return;
        }
        if (_instance._overlayMarkers == null)
        {
            Shader shader = _instance._overlay.Material.shader;
            _instance._overlayMarkers = new LineBatch(_instance.transform, shader, false, MeshTopology.Triangles);
            _instance._depthMarkers = new LineBatch(_instance.transform, shader, true, MeshTopology.Triangles);
        }
        LineBatch batch = depthTest ? _instance._depthMarkers : _instance._overlayMarkers;
        float halfSize = size * 0.5f;
        for (int i = 0; i < _cubeIndices.Length; i += 3)
        {
            Vector3 a = GetCubeCorner(center, halfSize, _cubeIndices[i]);
            Vector3 b = GetCubeCorner(center, halfSize, _cubeIndices[i + 1]);
            Vector3 c = GetCubeCorner(center, halfSize, _cubeIndices[i + 2]);
            batch.Add(a, b, color, duration, c);
        }
    }

    private static readonly int[] _cubeIndices =
    {
        0, 1, 3, 0, 3, 2,
        4, 6, 7, 4, 7, 5,
        0, 2, 6, 0, 6, 4,
        1, 5, 7, 1, 7, 3,
        0, 4, 5, 0, 5, 1,
        2, 3, 7, 2, 7, 6,
    };
    /// <summary>
    /// 비트로 지정한 축별 부호로 정육면체의 월드 꼭짓점을 계산한다.
    /// </summary>
    private static Vector3 GetCubeCorner(Vector3 center, float halfSize, int index)
    {
        return center + new Vector3((index & 1) == 0 ? -halfSize : halfSize, (index & 2) == 0 ? -halfSize : halfSize, (index & 4) == 0 ? -halfSize : halfSize);
    }
    /// <summary>
    /// 플레이 중 공통 렌더링 자원을 준비한다.
    /// </summary>
    private static bool EnsureInstance()
    {
        if (!Application.isPlaying)
        {
            return false;
        }
        if (_instance == null)
        {
            // Resources에 셰이더를 두어 빌드에서도 참조가 유지되도록 한다.
            Shader shader = Resources.Load<Shader>("PhysicsQueryDebug");
            if (shader == null || !shader.isSupported)
            {
                Debug.LogError("PhysicsQueryDebug 셰이더를 로드할 수 없거나 현재 플랫폼에서 지원하지 않습니다.");
                return false;
            }

            GameObject root = new GameObject("PhysicsQueryDebugRenderer") { hideFlags = HideFlags.DontSave };
            DontDestroyOnLoad(root);
            _instance = root.AddComponent<PhysicsQueryDebugRenderer>();
            _instance._overlay = new LineBatch(root.transform, shader, false);
            _instance._depthTest = new LineBatch(root.transform, shader, true);
        }

        return true;
    }
    /// <summary>
    /// 소유한 자원을 해제하고 재로드 이전 메시와 머티리얼도 자식 렌더러 참조로 정리한다.
    /// </summary>
    internal void DisposeResources(bool isImmediate)
    {
        _overlay?.Dispose(isImmediate);
        _depthTest?.Dispose(isImmediate);
        _overlayMarkers?.Dispose(isImmediate);
        _depthMarkers?.Dispose(isImmediate);
        _overlay = null;
        _depthTest = null;
        _overlayMarkers = null;
        _depthMarkers = null;
        foreach (MeshFilter filter in GetComponentsInChildren<MeshFilter>(true))
        {
            Mesh mesh = filter.sharedMesh;
            filter.sharedMesh = null;
            if (mesh != null && (mesh.hideFlags & HideFlags.DontSave) == HideFlags.DontSave) DestroyResource(mesh, isImmediate);
        }
        foreach (MeshRenderer renderer in GetComponentsInChildren<MeshRenderer>(true))
        {
            renderer.enabled = false;
            Material material = renderer.sharedMaterial;
            renderer.sharedMaterial = null;
            if (material != null && (material.hideFlags & HideFlags.DontSave) == HideFlags.DontSave) DestroyResource(material, isImmediate);
        }
    }

    private static void DestroyResource(Object resource, bool isImmediate)
    {
        if (resource == null) return;
        if (isImmediate || !Application.isPlaying) DestroyImmediate(resource);
        else Destroy(resource);
    }
    /// <summary>
    /// 선분의 월드 좌표, 색상과 최초 표시 프레임 및 만료 시간을 보관한다.
    /// </summary>
    private struct Line
    {
        public Vector3 start;
        public Vector3 end;
        public Vector3 third;
        public Color color;
        public double expiresAt;
        public bool isTimed;
        public int firstRenderedFrame;
    }
    /// <summary>
    /// 같은 깊이 검사 옵션의 선들을 재사용 메시 하나로 묶어 렌더링한다.
    /// </summary>
    private sealed class LineBatch
    {
        private readonly List<Line> _lines = new();
        private readonly List<Vector3> _vertices = new();
        private readonly List<Color> _colors = new();
        private readonly List<int> _indices = new();
        private readonly Mesh _mesh;
        private readonly Material _material;
        private readonly MeshRenderer _renderer;
        private readonly MeshTopology _topology;
        public Material Material => _material;

        public LineBatch(Transform parent, Shader shader, bool depthTest, MeshTopology topology = MeshTopology.Lines)
        {
            _topology = topology;
            GameObject child = new GameObject((depthTest ? "DepthTest" : "Overlay") + topology) { hideFlags = HideFlags.DontSave };
            child.transform.SetParent(parent, false);
            _mesh = new Mesh { name = child.name, hideFlags = HideFlags.DontSave, indexFormat = IndexFormat.UInt32 };
            _mesh.MarkDynamic();
            _material = new Material(shader) { hideFlags = HideFlags.DontSave };
            _material.SetInt("_ZTest", (int)(depthTest ? CompareFunction.LessEqual : CompareFunction.Always));
            child.AddComponent<MeshFilter>().sharedMesh = _mesh;
            _renderer = child.AddComponent<MeshRenderer>();
            _renderer.sharedMaterial = _material;
            _renderer.shadowCastingMode = ShadowCastingMode.Off;
            _renderer.receiveShadows = false;
            _renderer.lightProbeUsage = LightProbeUsage.Off;
            _renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            _renderer.allowOcclusionWhenDynamic = false;
            _renderer.enabled = false;
        }
        /// <summary>
        /// 표시 중 설정 변경의 영향을 받지 않도록 선과 수명을 값으로 저장한다.
        /// </summary>
        public void Add(Vector3 start, Vector3 end, Color color, float duration, Vector3 third = default)
        {
            _lines.Add(new Line
            {
                start = start,
                end = end,
                third = third,
                color = color,
                isTimed = duration > 0f,
                expiresAt = Time.timeAsDouble + duration,
                firstRenderedFrame = -1,
            });
        }
        /// <summary>
        /// 첫 표시를 보장하고 만료된 선을 제외하여 정점과 인덱스를 갱신한다.
        /// </summary>
        public void UpdateMesh()
        {
            if (_lines.Count == 0) return;
            _vertices.Clear();
            _colors.Clear();
            _indices.Clear();
            int remaining = 0;
            int frame = Time.frameCount;
            double now = Time.timeAsDouble;

            for (int i = 0; i < _lines.Count; i++)
            {
                Line line = _lines[i];
                bool hasRendered = line.firstRenderedFrame >= 0;
                bool isExpired = line.isTimed ? now >= line.expiresAt : frame > line.firstRenderedFrame;
                if (hasRendered && isExpired) continue;
                if (!hasRendered) line.firstRenderedFrame = frame;
                _lines[remaining++] = line;
                _indices.Add(_vertices.Count);
                _vertices.Add(line.start);
                _colors.Add(line.color);
                _indices.Add(_vertices.Count);
                _vertices.Add(line.end);
                _colors.Add(line.color);
                if (_topology == MeshTopology.Triangles)
                {
                    _indices.Add(_vertices.Count);
                    _vertices.Add(line.third);
                    _colors.Add(line.color);
                }
            }

            if (remaining < _lines.Count) _lines.RemoveRange(remaining, _lines.Count - remaining);
            _mesh.Clear();
            _renderer.enabled = remaining > 0;
            if (remaining == 0) return;
            _mesh.SetVertices(_vertices);
            _mesh.SetColors(_colors);
            _mesh.SetIndices(_indices, _topology, 0);
            _mesh.RecalculateBounds();
        }
        /// <summary>
        /// 생성한 네이티브 렌더링 자원을 해제한다.
        /// </summary>
        public void Dispose(bool isImmediate)
        {
            Clear();
            DestroyResource(_mesh, isImmediate);
            DestroyResource(_material, isImmediate);
        }
        /// <summary>
        /// 보관 중인 선을 제거하고 렌더러와 메시를 즉시 비운다.
        /// </summary>
        public void Clear()
        {
            _lines.Clear();
            if (_renderer != null) _renderer.enabled = false;
            if (_mesh != null) _mesh.Clear();
        }
    }
}
