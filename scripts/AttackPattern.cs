namespace Kugyou.scripts;

// Basic attack pattern builder
public class AttackPattern
{
	public string AnimationName { get; }
	public float Cooldown { get; }
	public int Damage { get; }

	public AttackPattern(string animationName, float cooldown, int damage)
	{
		AnimationName = animationName;
		Cooldown = cooldown;
		Damage = damage;
	}
}
