using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>Controls the objects already placed in the Potion Mortar demo scene.</summary>
public class PotionMortarDemo : MonoBehaviour
{
    private enum Station { Shelf, Mortar, Cauldron }

    private sealed class Ingredient
    {
        public string name;
        public Color color;
        public float grind;
        public GameObject visual;
        public Station station;
        public Vector3 shelfPosition;
        public Ingredient(string name, Color color, GameObject visual, Vector3 shelfPosition)
        {
            this.name = name; this.color = color; this.visual = visual;
            this.shelfPosition = shelfPosition; station = Station.Shelf;
        }
    }

    private readonly List<Ingredient> ingredients = new List<Ingredient>();
    private readonly string[] ingredientNames = { "Raíz lunar", "Seta azul", "Pétalo ígneo" };
    private readonly Color[] ingredientColors = {
        new Color(0.48f, 0.72f, 0.42f), new Color(0.28f, 0.55f, 0.92f), new Color(0.95f, 0.34f, 0.12f)
    };
    [Header("Ajustes del mortero")]
    public Transform mortarCenter;
    [Min(0.1f)] public float mortarRadius = 0.55f;
    [Range(0f, 80f)] public float maxRotationDegrees = 35f;

    private readonly Vector3 cauldronCenter = new Vector3(1.35f, 0.65f, 0.2f);
    private const float CauldronRadius = 0.95f;
    private const float RequiredGrind = 55f;
    private const float RequiredStir = 100f;

    private Camera sceneCamera;
    private GameObject pestle;
    private GameObject ladle;
    private GameObject mortarMixture;
    private Renderer mortarMixtureRenderer;
    private GameObject cauldronMixture;
    private Renderer cauldronMixtureRenderer;
    private Ingredient heldIngredient;
    private bool draggingPestle;
    private bool draggingLadle;
    private Vector3 dragOffset;
    private Vector3 pestleDragOffset;
    private Vector3 ladleDragOffset;
    private Vector3 previousMouse;
    private float cauldronStir;
    private int score;
    private string status = "Recoge los ingredientes y arrástralos al mortero.";
    private Rect panelRect;

    private void Start()
    {
        sceneCamera = Camera.main;
        pestle = FindSceneObject("Mano del mortero");
        ladle = FindSceneObject("Cucharón");
        mortarMixture = FindSceneObject("Mezcla del mortero");
        cauldronMixture = FindSceneObject("Mezcla del caldero");

        if (sceneCamera == null || mortarCenter == null || pestle == null || ladle == null || mortarMixture == null || cauldronMixture == null)
        {
            Debug.LogError("La escena necesita Main Camera, Mortero, Mano del mortero, Cucharón y los visuales de mezcla.", this);
            enabled = false;
            return;
        }

        mortarMixtureRenderer = mortarMixture.GetComponent<Renderer>();
        cauldronMixtureRenderer = cauldronMixture.GetComponent<Renderer>();
        for (int i = 0; i < ingredientNames.Length; i++)
        {
            GameObject visual = FindSceneObject("Ingrediente · " + ingredientNames[i]);
            if (visual == null)
            {
                Debug.LogError("Falta el ingrediente en la escena: " + ingredientNames[i], this);
                enabled = false;
                return;
            }
            ingredients.Add(new Ingredient(ingredientNames[i], ingredientColors[i], visual, visual.transform.position));
        }
    }

    private GameObject FindSceneObject(string objectName)
    {
        GameObject[] sceneObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject candidate in sceneObjects)
        {
            if (candidate.name == objectName && candidate.scene == gameObject.scene)
                return candidate;
        }
        return null;
    }

    private void Update()
    {
        HandleInput();
        AnimateTools();
        UpdateMixtureVisuals();
    }

    private void OnDrawGizmos()
    {
        if (mortarCenter == null) return;

        Vector3 center = mortarCenter.position;
        float radius = Mathf.Max(0.1f, mortarRadius);
        float angle = Mathf.Clamp(maxRotationDegrees, 0f, 80f);
        Gizmos.color = new Color(0.25f, 0.9f, 0.85f, 0.95f);
        DrawCircleXZ(center + Vector3.up * 0.35f, radius, 48);
        Gizmos.DrawSphere(center + Vector3.up * 0.35f, 0.055f);

        Vector3 pivot = center + Vector3.up * 0.48f;
        float arcLength = 0.72f;
        Gizmos.color = new Color(1f, 0.68f, 0.2f, 0.95f);
        DrawRotationArc(pivot, Vector3.forward, angle, arcLength, 24);
        DrawRotationArc(pivot, Vector3.right, angle, arcLength, 24);
        Gizmos.DrawLine(pivot, pivot + Quaternion.AngleAxis(angle, Vector3.forward) * Vector3.up * arcLength);
        Gizmos.DrawLine(pivot, pivot + Quaternion.AngleAxis(-angle, Vector3.forward) * Vector3.up * arcLength);
        Gizmos.DrawLine(pivot, pivot + Quaternion.AngleAxis(angle, Vector3.right) * Vector3.up * arcLength);
        Gizmos.DrawLine(pivot, pivot + Quaternion.AngleAxis(-angle, Vector3.right) * Vector3.up * arcLength);
    }

    private void DrawCircleXZ(Vector3 center, float radius, int segments)
    {
        Vector3 previous = center + new Vector3(radius, 0f, 0f);
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            Vector3 next = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            Gizmos.DrawLine(previous, next);
            previous = next;
        }
    }

    private void DrawRotationArc(Vector3 pivot, Vector3 axis, float angle, float length, int segments)
    {
        Vector3 previous = pivot + Quaternion.AngleAxis(-angle, axis) * Vector3.up * length;
        for (int i = 1; i <= segments; i++)
        {
            float step = Mathf.Lerp(-angle, angle, i / (float)segments);
            Vector3 next = pivot + Quaternion.AngleAxis(step, axis) * Vector3.up * length;
            Gizmos.DrawLine(previous, next);
            previous = next;
        }
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.R)) ResetBatch();

        if (Input.GetMouseButtonDown(0))
        {
            if (PointerOverPanel(Input.mousePosition)) return;
            Ray ray = sceneCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                if (hit.collider.gameObject == pestle)
                {
                    draggingPestle = true;
                    if (TryGetPlanePoint(Input.mousePosition, pestle.transform.position.y, out Vector3 pestlePoint))
                        pestleDragOffset = pestle.transform.position - pestlePoint;
                    previousMouse = Input.mousePosition;
                    return;
                }

                if (hit.transform == ladle.transform || hit.transform.IsChildOf(ladle.transform))
                {
                    draggingLadle = true;
                    if (TryGetPlanePoint(Input.mousePosition, ladle.transform.position.y, out Vector3 ladlePoint))
                        ladleDragOffset = ladle.transform.position - ladlePoint;
                    previousMouse = Input.mousePosition;
                    return;
                }

                Ingredient ingredient = FindIngredient(hit.collider.gameObject);
                if (ingredient != null)
                {
                    heldIngredient = ingredient;
                    dragOffset = ingredient.visual.transform.position - hit.point;
                    previousMouse = Input.mousePosition;
                    status = "Arrastra " + ingredient.name + " al mortero o al caldero.";
                    return;
                }
            }

            previousMouse = Input.mousePosition;
        }

        if (Input.GetMouseButton(0) && (heldIngredient != null || draggingPestle || draggingLadle))
        {
            Vector3 mouse = Input.mousePosition;
            Vector3 point;
            if (heldIngredient != null && TryGetWorkPlanePoint(mouse, out point))
            {
                heldIngredient.visual.transform.position = point + dragOffset;
                WorkDraggedIngredient(heldIngredient, point, Vector3.Distance(mouse, previousMouse));
            }
            else if (draggingPestle && TryGetPlanePoint(mouse, pestle.transform.position.y, out point))
            {
                Vector3 target = point + pestleDragOffset;
                Vector3 center = mortarCenter.position;
                Vector2 offset = new Vector2(target.x - center.x, target.z - center.z);
                float travelRadius = Mathf.Max(0.1f, mortarRadius) * 0.72f;
                if (offset.magnitude > travelRadius) offset = offset.normalized * travelRadius;
                pestle.transform.position = new Vector3(center.x + offset.x, pestle.transform.position.y, center.z + offset.y);
                WorkWithPestle(pestle.transform.position, Vector3.Distance(mouse, previousMouse));
            }
            else if (draggingLadle && TryGetPlanePoint(mouse, ladle.transform.position.y, out point))
            {
                Vector3 target = point + ladleDragOffset;
                Vector2 offset = new Vector2(target.x - cauldronCenter.x, target.z - cauldronCenter.z);
                float travelRadius = CauldronRadius * 0.72f;
                if (offset.magnitude > travelRadius) offset = offset.normalized * travelRadius;
                ladle.transform.position = new Vector3(cauldronCenter.x + offset.x, ladle.transform.position.y, cauldronCenter.z + offset.y);
                WorkWithLadle(ladle.transform.position, Vector3.Distance(mouse, previousMouse));
            }
            previousMouse = mouse;
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (heldIngredient != null) DropIngredient(heldIngredient);
            heldIngredient = null;
            draggingPestle = false;
            draggingLadle = false;
        }
    }

    private Ingredient FindIngredient(GameObject hitObject)
    {
        for (int i = 0; i < ingredients.Count; i++) if (ingredients[i].visual == hitObject) return ingredients[i];
        return null;
    }

    private bool TryGetWorkPlanePoint(Vector3 screenPosition, out Vector3 point)
    {
        Ray ray = sceneCamera.ScreenPointToRay(screenPosition);
        Plane plane = new Plane(Vector3.up, new Vector3(0, 0.48f, 0));
        if (plane.Raycast(ray, out float distance)) { point = ray.GetPoint(distance); return true; }
        point = Vector3.zero; return false;
    }

    private void WorkDraggedIngredient(Ingredient ingredient, Vector3 point, float movement)
    {
        if (ingredient.station != Station.Mortar || !InsideMortar(point)) return;
        float strength = Mathf.Clamp(movement / 38f, 0.2f, 1.5f);
        ingredient.grind = Mathf.Min(100f, ingredient.grind + strength * 1.1f);
        status = "Triturando " + ingredient.name + ": " + Mathf.RoundToInt(ingredient.grind) + "%";
    }

    private void WorkWithPestle(Vector3 point, float movement)
    {
        if (!InsideMortar(point)) return;
        Ingredient nearest = FindNearestMortarIngredient(point);
        if (nearest == null) return;
        nearest.grind = Mathf.Min(100f, nearest.grind + Mathf.Clamp(movement / 42f, 0.15f, 1.2f) * 0.8f);
        status = "Triturando " + nearest.name + ": " + Mathf.RoundToInt(nearest.grind) + "%";
    }

    private void WorkWithLadle(Vector3 point, float movement)
    {
        if (!InsideCauldron(point)) return;
        cauldronStir = Mathf.Min(100f, cauldronStir + Mathf.Clamp(movement / 65f, 0.1f, 1.4f));
        status = "Removiendo en el caldero: " + Mathf.RoundToInt(cauldronStir) + "%";
    }

    private bool TryGetPlanePoint(Vector3 screenPosition, float height, out Vector3 point)
    {
        Ray ray = sceneCamera.ScreenPointToRay(screenPosition);
        Plane plane = new Plane(Vector3.up, new Vector3(0, height, 0));
        if (plane.Raycast(ray, out float distance)) { point = ray.GetPoint(distance); return true; }
        point = Vector3.zero; return false;
    }

    private Ingredient FindNearestMortarIngredient(Vector3 point)
    {
        Ingredient nearest = null; float nearestDistance = 0.55f;
        foreach (Ingredient ingredient in ingredients)
        {
            if (ingredient.station != Station.Mortar) continue;
            float distance = Vector2.Distance(new Vector2(ingredient.visual.transform.position.x, ingredient.visual.transform.position.z), new Vector2(point.x, point.z));
            if (distance < nearestDistance) { nearest = ingredient; nearestDistance = distance; }
        }
        return nearest;
    }

    private void DropIngredient(Ingredient ingredient)
    {
        Vector3 position = ingredient.visual.transform.position;
        if (InsideMortar(position))
        {
            if (CountAt(Station.Mortar) >= 3 && ingredient.station != Station.Mortar) { ReturnToShelf(ingredient); status = "El mortero admite hasta 3 ingredientes."; return; }
            ingredient.station = Station.Mortar;
            ingredient.visual.transform.position = NextStationPosition(Station.Mortar, CountAt(Station.Mortar) - 1);
            status = "" + ingredient.name + " en el mortero. Arrástralo para triturar.";
        }
        else if (InsideCauldron(position))
        {
            if (CountAt(Station.Cauldron) >= 2 && ingredient.station != Station.Cauldron) { ReturnToShelf(ingredient); status = "El caldero admite hasta 2 ingredientes."; return; }
            ingredient.station = Station.Cauldron;
            ingredient.visual.transform.position = NextStationPosition(Station.Cauldron, CountAt(Station.Cauldron) - 1);
            status = "" + ingredient.name + " en el caldero.";
        }
        else ReturnToShelf(ingredient);
    }

    private int CountAt(Station station)
    {
        int count = 0; foreach (Ingredient ingredient in ingredients) if (ingredient.station == station) count++;
        return count;
    }

    private Vector3 NextStationPosition(Station station, int slot)
    {
        Vector3 center = station == Station.Mortar ? mortarCenter.position : cauldronCenter;
        float angle = slot * 2.4f;
        return new Vector3(center.x + Mathf.Cos(angle) * 0.32f, 1.17f, center.z + Mathf.Sin(angle) * 0.32f);
    }

    private void ReturnToShelf(Ingredient ingredient)
    {
        ingredient.station = Station.Shelf;
        ingredient.visual.transform.position = ingredient.shelfPosition;
    }

    private bool InsideMortar(Vector3 point)
    {
        Vector3 center = mortarCenter.position;
        float radius = Mathf.Max(0.1f, mortarRadius);
        return new Vector2(point.x - center.x, point.z - center.z).sqrMagnitude <= radius * radius;
    }

    private bool InsideCauldron(Vector3 point)
    {
        return new Vector2(point.x - cauldronCenter.x, point.z - cauldronCenter.z).sqrMagnitude <= CauldronRadius * CauldronRadius;
    }

    private bool PointerOverPanel(Vector3 mousePosition)
    {
        Vector2 point = new Vector2(mousePosition.x, Screen.height - mousePosition.y);
        return panelRect.Contains(point);
    }

    private void AnimateTools()
    {
        if (!draggingPestle)
        {
            pestle.transform.rotation = Quaternion.Euler(0f, 0f, -18f);
            return;
        }

        Vector3 center = mortarCenter.position;
        Vector3 offset = pestle.transform.position - center;
        float radius = Mathf.Max(0.1f, mortarRadius) * 0.72f;
        float limit = Mathf.Clamp(maxRotationDegrees, 0f, 80f);
        float tiltX = Mathf.Clamp(-offset.z / radius * limit, -limit, limit);
        float tiltZ = Mathf.Clamp(-18f + offset.x / radius * limit, -limit, limit);
        pestle.transform.rotation = Quaternion.Euler(tiltX, 0f, tiltZ);
    }

    private void UpdateMixtureVisuals()
    {
        UpdateMixture(mortarMixture, mortarMixtureRenderer, Station.Mortar, 0f);
        UpdateMixture(cauldronMixture, cauldronMixtureRenderer, Station.Cauldron, cauldronStir);
    }

    private void UpdateMixture(GameObject visual, Renderer renderer, Station station, float stir)
    {
        int count = CountAt(station);
        visual.SetActive(count > 0);
        if (count == 0) return;
        Color average = Color.black; float grind = 0;
        foreach (Ingredient ingredient in ingredients)
        {
            if (ingredient.station != station) continue;
            average += ingredient.color; grind += ingredient.grind / 100f;
        }
        average /= count;
        Color mixTint = station == Station.Mortar ? new Color(0.47f, 0.22f, 0.72f) : new Color(0.24f, 0.78f, 0.65f);
        renderer.material.color = Color.Lerp(average, mixTint, stir / 160f);
        visual.transform.localScale = new Vector3(1.1f, 0.05f + grind / count * 0.08f + stir / 1000f, 1.1f);
    }

    private void Brew()
    {
        if (CountAt(Station.Cauldron) != 2) { status = "Pasa exactamente 2 ingredientes al caldero."; return; }
        if (CountAt(Station.Mortar) > 0) { status = "El mortero aún tiene ingredientes: muévelos al caldero."; return; }
        if (cauldronStir < RequiredStir) { status = "Remueve el caldero hasta llegar al 100%."; return; }

        Ingredient first = null, second = null;
        foreach (Ingredient ingredient in ingredients)
        {
            if (ingredient.station != Station.Cauldron) continue;
            if (first == null) first = ingredient; else second = ingredient;
        }
        if (first.grind < RequiredGrind || second.grind < RequiredGrind)
        {
            status = "Tritura cada ingrediente en el mortero hasta el 55% antes de mezclarlos."; return;
        }

        string recipe = GetRecipe(first.name, second.name);
        if (recipe == null)
        {
            status = "Receta fallida. La mezcla no coincide con las recetas conocidas.";
            ResetIngredients(false);
            return;
        }

        score += 10;
        status = "¡Poción de " + recipe + " correcta! +10 puntos.";
        ResetIngredients(false);
    }

    private string GetRecipe(string first, string second)
    {
        bool hasRoot = first == "Raíz lunar" || second == "Raíz lunar";
        bool hasMushroom = first == "Seta azul" || second == "Seta azul";
        bool hasPetal = first == "Pétalo ígneo" || second == "Pétalo ígneo";
        if (hasRoot && hasMushroom) return "curación";
        if (hasMushroom && hasPetal) return "maná";
        if (hasRoot && hasPetal) return "velocidad";
        return null;
    }

    private void ResetIngredients(bool showMessage)
    {
        foreach (Ingredient ingredient in ingredients)
        {
            ingredient.grind = 0;
            ReturnToShelf(ingredient);
        }
        cauldronStir = 0;
        if (showMessage) status = "Mesa reiniciada. Recoge dos ingredientes.";
    }

    private void ResetBatch()
    {
        score = 0;
        ResetIngredients(true);
    }

    private void OnGUI()
    {
        panelRect = new Rect(18, 18, 385, 404);
        GUI.Box(panelRect, "");
        GUIStyle title = new GUIStyle(GUI.skin.label) { fontSize = 23, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
        GUIStyle label = new GUIStyle(GUI.skin.label) { fontSize = 14, normal = { textColor = new Color(0.92f, 0.89f, 0.98f) } };
        GUIStyle small = new GUIStyle(label) { fontSize = 12 };
        GUI.Label(new Rect(34, 27, 350, 32), "TALLER DE POCIONES", title);
        GUI.Label(new Rect(34, 62, 330, 25), "Puntos: " + score, label);
        GUI.Label(new Rect(34, 88, 345, 35), status, small);

        GUI.Label(new Rect(34, 127, 345, 23), "RECETAS · 10 PUNTOS CADA UNA", label);
        GUI.Label(new Rect(34, 151, 345, 22), "Curación: Raíz lunar + Seta azul", small);
        GUI.Label(new Rect(34, 173, 345, 22), "Maná: Seta azul + Pétalo ígneo", small);
        GUI.Label(new Rect(34, 195, 345, 22), "Velocidad: Raíz lunar + Pétalo ígneo", small);

        GUI.Label(new Rect(34, 227, 345, 22), "Mortero: " + StationSummary(Station.Mortar), small);
        GUI.Label(new Rect(34, 249, 345, 22), "Triturado: " + Mathf.RoundToInt(AverageGrind()) + "%   Caldero: " + Mathf.RoundToInt(cauldronStir) + "%", small);
        GUI.Label(new Rect(34, 271, 345, 22), "Caldero: " + StationSummary(Station.Cauldron), small);
        GUI.backgroundColor = new Color(0.72f, 0.49f, 0.27f);
        if (GUI.Button(new Rect(34, 305, 335, 34), "Preparar poción")) Brew();
        GUI.backgroundColor = new Color(0.31f, 0.27f, 0.37f);
        if (GUI.Button(new Rect(34, 346, 335, 32), "Reiniciar mesa  (R)")) ResetBatch();
        GUI.Label(new Rect(34, 382, 350, 22), "Mazo: triturar. Cucharón: mezclar el caldero.", small);
        DrawStationLabel("MORTERO\nTRITURAR", mortarCenter.position + Vector3.up * 1.6f, new Color(0.77f, 0.65f, 1f));
        DrawStationLabel("CALDERO\nMEZCLAR", cauldronCenter + Vector3.up * 2.0f, new Color(0.52f, 0.94f, 0.83f));
    }

    private void DrawStationLabel(string text, Vector3 worldPosition, Color color)
    {
        Vector3 screen = sceneCamera.WorldToScreenPoint(worldPosition);
        if (screen.z <= 0f) return;
        Rect rect = new Rect(screen.x - 66f, Screen.height - screen.y - 22f, 132f, 44f);
        Color previousColor = GUI.color;
        GUI.color = color;
        GUIStyle style = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            wordWrap = true
        };
        style.normal.textColor = Color.white;
        GUI.Box(rect, text, style);
        GUI.color = previousColor;
    }

    private string StationSummary(Station station)
    {
        StringBuilder result = new StringBuilder();
        foreach (Ingredient ingredient in ingredients)
        {
            if (ingredient.station != station) continue;
            if (result.Length > 0) result.Append(", ");
            result.Append(ingredient.name);
        }
        return result.Length == 0 ? "vacío" : result.ToString();
    }

    private float AverageGrind()
    {
        float total = 0; int count = 0;
        foreach (Ingredient ingredient in ingredients)
        {
            if (ingredient.station != Station.Cauldron) continue;
            total += ingredient.grind; count++;
        }
        return count == 0 ? 0 : total / count;
    }
}
