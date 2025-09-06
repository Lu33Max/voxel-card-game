using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Unit))]
public class UnitAnimator : MonoBehaviour
{
    [SerializeField] private float stepDuration = 0.15f;
    [SerializeField] private float moveArcHeight = 0.3f;

    private AudioSource _sfxSource = null!;
    
    private void Awake()
    {
        _sfxSource = GetComponent<AudioSource>();
    }

    /// <summary> Makes the unit step onto the given neighbour position </summary>
    /// <param name="targetPos"> World position of the neighbour tile </param>
    public IEnumerator PlayMoveAnimation(Vector3 targetPos)
    {
        var startingPos = transform.position;
        var direction = targetPos - startingPos;
        direction.y = 0;
        
        var targetRotation = direction != Vector3.zero ? Quaternion.LookRotation(direction) : transform.rotation;
        AudioManager.PlaySfx(_sfxSource, AudioManager.Instance.UnitMove);
        
        float elapsedTime = 0;
        var unitTransform = transform;
        
        while (elapsedTime < stepDuration)
        {
            if (Quaternion.Angle(transform.rotation, targetRotation) > 0.1f)
                unitTransform.rotation = Quaternion.Lerp(unitTransform.rotation, targetRotation, 15 * Time.deltaTime);
            
            var progression = elapsedTime / stepDuration;
            var newPos = Vector3.Lerp(startingPos, targetPos, progression);
            
            // Parabolic movement
            var baseHeight = Mathf.Lerp(startingPos.y, targetPos.y, progression);
            var height = 4 * moveArcHeight * progression * (1 - progression);
            newPos.y = baseHeight + height;

            unitTransform.position = newPos;
            elapsedTime += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }
        
        unitTransform.rotation = targetRotation;
        unitTransform.position = targetPos;
    }
    
    /// <summary> Play the animation of the unit colliding mid way with a different unit </summary>
    /// <param name="targetPos"> The tile the unit was supposed to step to, but got blocked </param>
    public IEnumerator PlayBlockedAnimation(Vector3 targetPos)
    {
        var startingPos = transform.position;
        var direction = targetPos - startingPos;
        direction.y = 0;
        
        var targetRotation = direction != Vector3.zero ? Quaternion.LookRotation(direction) : transform.rotation;
        AudioManager.PlaySfx(_sfxSource, AudioManager.Instance.UnitMove);
        
        float elapsedTime = 0;
        var unitTransform = transform;
        
        while (elapsedTime < stepDuration / 2)
        {
            if (Quaternion.Angle(transform.rotation, targetRotation) > 0.1f)
                unitTransform.rotation = Quaternion.Lerp(unitTransform.rotation, targetRotation, 15 * Time.deltaTime);
            
            var progression = elapsedTime / stepDuration;
            var newPos = Vector3.Lerp(startingPos, targetPos, progression);
            
            // Parabolic movement
            var baseHeight = Mathf.Lerp(startingPos.y, targetPos.y, progression);
            var height = 4 * moveArcHeight * progression * (1 - progression);
            newPos.y = baseHeight + height;

            unitTransform.position = newPos;
            elapsedTime += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }
        
        while (elapsedTime < stepDuration)
        {
            if (Quaternion.Angle(transform.rotation, targetRotation) > 0.1f)
                unitTransform.rotation = Quaternion.Lerp(unitTransform.rotation, targetRotation, 15 * Time.deltaTime);
            
            var progression = elapsedTime / stepDuration;
            var newPos = Vector3.Lerp(targetPos, startingPos, progression);
            
            // Parabolic movement
            var baseHeight = Mathf.Lerp(targetPos.y, startingPos.y, progression);
            var height = 4 * moveArcHeight * progression * (1 - progression);
            newPos.y = baseHeight + height;

            unitTransform.position = newPos;
            elapsedTime += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }

        unitTransform.rotation = targetRotation;
        unitTransform.position = startingPos;
    }

    /// <summary> Plays the attack animation of the unit, facing in the direction of the given attack </summary>
    /// <param name="attack"> Data for the attack to animate </param>
    public IEnumerator PlayAttackAnimation(Attack attack)
    {
        var startingPos = transform.position;
        var direction = GridManager.Instance.GridToWorldPosition(attack.Tiles[0])!.Value - startingPos;
        direction.y = 0;
        
        var targetRotation = Quaternion.LookRotation(direction);
        
        var unitTransform = transform;
        var elapsedTime = 0f;

        while (elapsedTime < stepDuration)
        {
            if (Quaternion.Angle(transform.rotation, targetRotation) > 0.1f)
                unitTransform.rotation = Quaternion.Lerp(unitTransform.rotation, targetRotation, 15 * Time.deltaTime);
            
            var newPos = unitTransform.position;
            var progression = elapsedTime / stepDuration;
            newPos.y = startingPos.y + 4 * moveArcHeight * progression * (1 - progression);
            
            unitTransform.position = newPos;
            
            elapsedTime += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }

        unitTransform.position = startingPos;
        unitTransform.rotation = targetRotation;
    }
}
