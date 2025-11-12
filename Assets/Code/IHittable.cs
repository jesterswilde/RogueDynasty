public enum HittableType {
    Character,
    Object
}
public interface IHittable {
    void GotHitBy(AttackData attack);
    HittableType HType { get; }
}
