using System;
using _Scripts.Player;
using Bellseboss.Pery.Scripts.Input;
using UnityEngine;

public class ScalableWall : MonoBehaviour
{
    [SerializeField] private GetDataWentCollisionWithPlayer targetFocus;
    [SerializeField] private float forceToGravitate;
    [SerializeField] private float lenghtOfArrow;
    private CharacterV2 _characterV2;
    void Start()
    {
        targetFocus.Configure();
        targetFocus.EnableCollider();
        targetFocus.CollisionEnter += TargetFocusOnCollisionEnter;
        targetFocus.CollisionExit += TargetFocusOnCollisionExit;
    }

    private void TargetFocusOnCollisionExit(GameObject obj, Vector3 point)
    {
        //Debug.Log($"ScalableWall: TargetFocusOnCollisionExit: obj: {obj.name}");
        _characterV2.ExitScalableWall();
        _characterV2 = null;
    }

    private void TargetFocusOnCollisionEnter(GameObject obj, Vector3 point)
    {
        _characterV2 = obj.GetComponent<CharacterV2>();
        _characterV2.TouchedScallableWall(true, forceToGravitate, transform.forward);
    }

    private void OnDrawGizmos()
    {
        //draw a line to front of object with gizmo
        Gizmos.color = Color.red;
        //Target to front of object
        var target = transform.position + transform.forward * lenghtOfArrow;
        Gizmos.DrawLine(transform.position, target);
        
        // Dibujar la punta de la flecha
        Vector3 rightArrowPoint = Quaternion.LookRotation(transform.forward) * Quaternion.Euler(0, 180 + 20, 0) * new Vector3(0, 0, 1);
        Vector3 leftArrowPoint = Quaternion.LookRotation(transform.forward) * Quaternion.Euler(0, 180 - 20, 0) * new Vector3(0, 0, 1);

        Gizmos.DrawLine(target, target + rightArrowPoint * (lenghtOfArrow/5));
        Gizmos.DrawLine(target, target + leftArrowPoint * (lenghtOfArrow/5));

    }
}
