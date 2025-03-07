using Unity.VisualScripting;
using UnityEngine;

public class AmmoInventory : MonoBehaviour
{
    [SerializeField] private int _revolverAmmoAmount;
    [SerializeField] private int _pistolAmmoAmount;
    [SerializeField] private int _shotgunAmmoAmount;
    [SerializeField] private int _assaultRifleAmmoAmount;
    
    #region Properties
    public int RevolverAmmoAmount => _revolverAmmoAmount;
    public int PistolAmmoAmount => _pistolAmmoAmount;
    public int ShotgunAmmoAmount => _shotgunAmmoAmount;
    public int AssaultRifleAmmoAmount => _assaultRifleAmmoAmount;
    #endregion

    public void ReduceAmmoAmount(Weapon.WeaponType type, int sizeToReduce)
    {
        switch (type)
        {
            case Weapon.WeaponType.Revolver:
                _revolverAmmoAmount -= sizeToReduce;
                //edgecases
                break;
            case Weapon.WeaponType.Pistol:
                _pistolAmmoAmount -= sizeToReduce;
                break;
            case Weapon.WeaponType.Shotgun:
                _shotgunAmmoAmount -= sizeToReduce;
                break;
            case Weapon.WeaponType.AssaultRifle:
                _assaultRifleAmmoAmount -= sizeToReduce;
                break;
            //TODO Add edgecases
        }   
    }

    public int ReturnCurrentAmmoAmount(Weapon.WeaponType type)
    {
        switch(type)
        {
            case Weapon.WeaponType.Revolver:
                return _revolverAmmoAmount;
                break;
            case Weapon.WeaponType.Pistol:
                return _pistolAmmoAmount;
                break;
            case Weapon.WeaponType.AssaultRifle:
                return _assaultRifleAmmoAmount;
                break;
            case Weapon.WeaponType.Shotgun:
                return _shotgunAmmoAmount;
                break;
            default: return 0;
        }
    }
}
