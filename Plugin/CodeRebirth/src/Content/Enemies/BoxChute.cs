using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace CodeRebirth.src.Content.Enemies;

public class BoxChute : MonoBehaviour
{
    [field: SerializeField]
    public float FallingSpeed { get; private set; } = 10f;

    [field: SerializeField]
    public AudioSource AudioSource { get; private set; }

    [field: SerializeField]
    public AudioClip AmbientDroppingSound { get; private set; }

    [field: SerializeField]
    public AudioClip LandingSound { get; private set; }

    [field: SerializeField]
    public UnityEvent OnLanding { get; private set; } = new();

    private Vector3 landingPosition = Vector3.zero;
    public void SetupBoxChute()
    {
        landingPosition = Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 500f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore) ? hit.point : transform.position;
        landingPosition = NavMesh.SamplePosition(landingPosition, out NavMeshHit navMeshHit, 20f, NavMesh.AllAreas) ? navMeshHit.position : landingPosition;

        Vector3 usableDirectionToFaceOnLanding = this.transform.forward;
        for (int i = 0; i < 4; i++)
        {
            if (!Physics.Raycast(landingPosition + Vector3.up * 0.33f, usableDirectionToFaceOnLanding, 3f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
            {
                Plugin.ExtendedLogging($"Found valid direction for box chute to face on landing: {usableDirectionToFaceOnLanding} from initial direction {this.transform.forward}");
                this.transform.forward = usableDirectionToFaceOnLanding;
                break;
            }

            usableDirectionToFaceOnLanding = Quaternion.Euler(0, 90, 0) * usableDirectionToFaceOnLanding;
        }
    }

    public void Start()
    {
        AudioSource.clip = AmbientDroppingSound;
        AudioSource.Play();
    }

    public void Update()
    {
        if (landingPosition == Vector3.zero)
        {
            return;
        }

        if (Vector3.Distance(transform.position, landingPosition) <= 0.5f)
        {
            this.transform.position = landingPosition;
            landingPosition = Vector3.zero;
            OnLanding.Invoke();
            AudioSource.Stop();
            AudioSource.PlayOneShot(LandingSound);
        }

        this.transform.position = Vector3.MoveTowards(this.transform.position, landingPosition, Time.deltaTime * FallingSpeed);
    }
}