// 这是一个接口，不是类，不需要挂载到物体上
public interface IDamageable
{
    // 任何实现这个接口的物体，必须拥有这个方法
    void TakeDamage(float amount);
}