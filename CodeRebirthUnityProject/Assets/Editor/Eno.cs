using System.Collections;
using UnityEngine;
using UnityEditor;

public class Eno : EditorWindow
{
    private enum States { first, second, showing, none };
    private States measurementState = States.none;
    private Vector3 first = Vector3.zero;
    private Vector3 second = Vector3.zero;


    [MenuItem("Tools/Measure Distance")]
    static void Init()
    {
        Eno window = (Eno)EditorWindow.GetWindow(typeof(Eno));
        window.minSize = new Vector2(215f, 22f);
        window.maxSize = new Vector2(1000f, 222f);
        window.position = new Rect(window.position.position, window.minSize);
    }
    void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }


    void OnGUI()
    {
        GUILayout.BeginHorizontal();
        switch (measurementState)
        {
            case States.first: GUILayout.Label("Pick first point"); break;
            case States.second: GUILayout.Label("Pick last point"); break;
            case States.showing: GUILayout.Label(Vector3.Distance(first, second).ToString()); break;
            default:
                if (GUILayout.Button("Measure")) { measurementState = States.first; }
                break;
        }
        if (GUILayout.Button("Restart")) Restart();
        GUILayout.EndHorizontal();
    }


    public void OnSceneGUI(SceneView sceneView)
    {
        if (measurementState.Equals(States.first) && DropPoint(out first))
        {
            measurementState = States.second;
        }
        else if (measurementState.Equals(States.first))
        {
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        }
        else if (measurementState.Equals(States.second) && DropPoint(out second))
        {
            measurementState = States.showing;
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        }
        else if (measurementState.Equals(States.second))
        {
            CheckCancel();
            Handles.SphereHandleCap(42, first, Quaternion.identity, 0.05f, EventType.Repaint);
            Ray mouseRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            if (Physics.Raycast(mouseRay, out RaycastHit hit, 100f))
            {
                Handles.DrawLine(first, hit.point);
                Handles.SphereHandleCap(42, hit.point, Quaternion.identity, 0.05f, EventType.Repaint);
            }
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        }
        else if (measurementState.Equals(States.showing))
        {
            CheckCancel();
            HandleUtility.Repaint();
            Handles.DrawLine(first, second);
            Handles.SphereHandleCap(42, first, Quaternion.identity, 0.05f, EventType.Repaint);
            Handles.SphereHandleCap(42, second, Quaternion.identity, 0.05f, EventType.Repaint);
            if (Event.current.type.Equals(EventType.MouseDown) && (Event.current?.button == 0))
            {
                Restart();
            }
            this.Repaint();
        }
        else HandleUtility.Repaint();
        SceneView.RepaintAll();
    }



    private void Restart()
    {
        measurementState = States.none;
        first = Vector3.zero;
        second = Vector3.zero;
    }

    private void CheckCancel()
    {
        Event e = Event.current;
        if (e.keyCode == KeyCode.Escape && e.type.Equals(EventType.KeyDown))
        {
            Restart();
            this.Repaint();
        }
    }


    private bool DropPoint(out Vector3 point)
    {
        point = Vector3.zero;
        Event e = Event.current;
        bool pointFound = false;
        if (e.button == 0 && e.type.Equals(EventType.MouseDown))
        {
            //Checks to find the selected point.
            Ray mouseRay = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            RaycastHit[] hits = Physics.RaycastAll(mouseRay, 200f);
            foreach (var hit in hits)
            {
                if (Vector3.Dot(hit.normal, mouseRay.direction) < 0)
                {
                    point = hit.point;
                    pointFound = true;
                    break;//This we want a point where the normal faces the camera.
                }
                else if (!pointFound)
                { //sets it to the nearest point without a normal facing the camera
                    point = hit.point;
                    pointFound = true;
                }
            }
        }
        return pointFound;
    }
}