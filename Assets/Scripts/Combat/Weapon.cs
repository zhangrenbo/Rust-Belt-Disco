using UnityEngine;

public class Weapon : MonoBehaviour
{
    public string weaponName; // ��������

    [Header("Bonus Stats")]
    [Tooltip("�������ӵĹ�����")]
    public int attackBonus;  // �������ӵĹ�����
    [Tooltip("�������ӵĹ����ٶ�")]
    public float speedBonus; // �������ӵĹ����ٶ�
    [Tooltip("�������ӵķ���ǿ��")]
    public int spellBonus;   // �������ӵķ���ǿ��

    [Header("Hitbox Settings")]
    [Tooltip("�Զ��� Hitbox Ԥ���壬��������ˣ���ʹ���Զ��幥��Ч��")]
    public GameObject hitboxPrefab;
    [Tooltip("Hitbox ����ʱ��")]
    public float hitboxDuration = 0.2f;
    [Tooltip("Hitbox ƫ����")]
    public Vector3 hitboxOffset = new Vector3(0, 0.5f, 1f);
    [Tooltip("Hitbox ������Χ")]
    public float hitboxRange = 3f;
    [Tooltip("Hitbox �����˺�")]
    public int hitboxDamage = 10;

    // �ж��Ƿ��������Զ��� Hitbox
    public bool HasCustomHitbox()
    {
        return hitboxPrefab != null;
    }

    // ��ѡ����չ PerformAttack ���������������о��������߼�
}