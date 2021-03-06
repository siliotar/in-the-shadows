using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Controller : MonoBehaviour
{
    private Vector3 _objectCenter;
    private bool _pressed = false;
    private LevelData _tempLevel;
    private bool _solved = false;
    private Vector3 _barycenter;
    private float _toleranceAlpha = 8.0f;
    private Quaternion[] _correctRotations;

    public static GameObject TempObject;
    public static List<GameObject> Objects;

    private void Start()
    {
        TempObject = GameObject.FindGameObjectWithTag("Object");
        _objectCenter.x = -2.5f;
        _objectCenter.y = 3.3f;
        _objectCenter.z = 0.0f;
        _tempLevel = Levels.data[SelectedLevel._lvl];
        _correctRotations = new Quaternion[_tempLevel.objects.Count];
    }

    private Vector3 CalculateBarycenter()
    {
        Vector3 result = Vector3.zero;
        float massSumm = 0.0f;
        for (int i = 0; i < Objects.Count; ++i)
        {
            result += Objects[i].transform.position * _tempLevel.objects[i].mass;
            massSumm += _tempLevel.objects[i].mass;
        }
        result /= massSumm;
        return result;
    }

    private void CheckSolve()
    {
        if (_tempLevel.objects.Count != 1)
        {
            Vector3 diffPos = Objects[0].transform.position - (_objectCenter + _tempLevel.objects[0].position);
            for (int i = 1; i < _tempLevel.objects.Count; ++i)
            {
                Vector3 tempDiffPos = Objects[i].transform.position - (_objectCenter + _tempLevel.objects[i].position);
                if ((diffPos - tempDiffPos).sqrMagnitude > 0.01)
                    return;
            }
        }
        foreach (Vector3 rotation in _tempLevel.additiontalCorrectRotations)
        {
            Quaternion quatRotation = Quaternion.Euler(rotation);
            for (int i = 0; i < _tempLevel.objects.Count; ++i)
            {
                Object tempObjectData = _tempLevel.objects[i];
                GameObject tempGameObject = Objects[i];
                Vector3 up = quatRotation * Vector3.up;
                if (tempObjectData.freeUp || Vector3.Angle(up, tempGameObject.transform.up) <= _toleranceAlpha)
                {
                    Vector3 right = quatRotation * Vector3.right;
                    Vector3 left = quatRotation * Vector3.left;
                    if (Vector3.Angle(right, tempGameObject.transform.right) <= _toleranceAlpha)
                        _correctRotations[i] = Quaternion.Euler(rotation);
                    else if (tempObjectData.mirrorTolerance && Vector3.Angle(left, tempGameObject.transform.right) <= _toleranceAlpha)
                        _correctRotations[i] = Quaternion.Euler(rotation) * Quaternion.Euler(0.0f, 180.0f, 0.0f);
                    else
                        break;
                    if (i + 1 >= _tempLevel.objects.Count)
                    {
                        Player.Levels[SelectedLevel._lvl] = LevelInfo.Solved;
                        if (SelectedLevel._lvl + 1 < Player.Levels.Length && Player.Levels[SelectedLevel._lvl + 1] == LevelInfo.Inactive)
                            Player.Levels[SelectedLevel._lvl + 1] = LevelInfo.Active;
                        SaveSystem.SavePlayer();
                        _solved = true;
                        _barycenter = CalculateBarycenter();
                        StartCoroutine(BackToMenu());
                    }
                }
                else
                    break;
            }
        }
    }

    private void ProcessInput()
    {
        if (!InGameMenu.Active() && Input.GetMouseButton(0))
        {
            if (!_pressed)
            {
                _pressed = true;
                return;
            }
            float deltaX = Input.GetAxis("Mouse X");
            float deltaY = Input.GetAxis("Mouse Y");
            if (_tempLevel.difficulty == 0 ||
                !(Input.GetKey(Controls.keys[Controls.moveParameter]) ||
                Input.GetKey(Controls.keys[Controls.rollParameter])))
                TempObject.transform.Rotate(0, -deltaX * Controls.mouseSensitivity * Time.deltaTime, 0, Space.World);
            if (_tempLevel.difficulty > 0)
            {
                if (!(Input.GetKey(Controls.keys[Controls.moveParameter]) ||
                    Input.GetKey(Controls.keys[Controls.rollParameter])))
                    TempObject.transform.Rotate(0, 0, deltaY * Controls.mouseSensitivity * Time.deltaTime, Space.World);
                else if (Input.GetKey(Controls.keys[Controls.rollParameter]))
                    TempObject.transform.Rotate(-deltaX * Controls.mouseSensitivity * Time.deltaTime, 0, 0, Space.World);
                else if (_tempLevel.difficulty > 1)
                {
                    if (Input.GetKey(Controls.keys[Controls.moveParameter]))
                    {
                        Vector3 translation;
                        translation.x = 0.0f;
                        translation.y = deltaY * Controls.mouseSensitivity * 0.01f * Time.deltaTime;
                        translation.z = deltaX * Controls.mouseSensitivity * 0.01f * Time.deltaTime;
                        TempObject.transform.Translate(translation, Space.World);
                        Vector3 diff = TempObject.transform.position - _objectCenter;
                        if (diff.sqrMagnitude > 1.0f)
                        {
                            diff.Normalize();
                            TempObject.transform.position = _objectCenter + diff;
                        }
                    }
                }
            }
            // Check that the level is completed
            CheckSolve();
        }
        else
            _pressed = false;
        if (Input.GetKeyDown(KeyCode.Escape))
            InGameMenu.SetActive(!InGameMenu.Active());
    }

    private void Update()
    {
        if (TempObject == null)
            return;
        if (!_solved)
            ProcessInput();
        else
        {
            int i = 0;
            foreach (GameObject tempObject in Objects)
            {
                // Correct position
                Vector3 position = Vector3.MoveTowards(tempObject.transform.position, _barycenter + _tempLevel.objects[i].position, 0.1f * Time.deltaTime);
                // Correct rotation
                Quaternion rotation = Quaternion.Lerp(tempObject.transform.rotation, _correctRotations[i], Time.deltaTime);

                tempObject.transform.SetPositionAndRotation(position, rotation);
                ++i;
            }
        }
    }

    private IEnumerator BackToMenu()
    {
        yield return new WaitForSeconds(2);
        InGameMenu.SetActive(true);
    }
}
