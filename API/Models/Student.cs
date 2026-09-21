namespace API.Models; 

/* Anemic Version
Public class Student{

public Guid Id {get;set;}
public string FullName {get; set; }
public List <string> Subjects {get; set;}

}
*/
//	A	class,	not	a	record	—	Student	is	an	ENTITY	(it	has	identity	and
//	behavior	over	time),	not	a	value	object	or	a	response	shape.
public class Student
{
    private readonly List<string> _subjectCodes = new();
    public Guid Id { get; }
    public string FullName { get; private set; }
    public string LearnerReferenceNumber { get; }
    //	Read-only	view	of	internal	state	—	callers	cannot	bypass	EnrollSubject()
    //	to	mutate	the	list	directly.
    public IReadOnlyCollection<string> SubjectCodes => _subjectCodes.AsReadOnly();
    public Student(string fullName, string learnerReferenceNumber)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Full	name	is	required.", nameof(fullName));
        if (string.IsNullOrWhiteSpace(learnerReferenceNumber))
            throw new ArgumentException("Learner	reference	number	is	required.", nameof(learnerReferenceNumber));
        Id = Guid.NewGuid();
        FullName = fullName;
        LearnerReferenceNumber = learnerReferenceNumber;
    }
    //	A	domain	method	—	behavior	lives	WITH	the	data	it	protects.
    public void EnrollSubject(string subjectCode)
    {
        if (string.IsNullOrWhiteSpace(subjectCode))
            throw new ArgumentException("Subject	code	is	required.", nameof(subjectCode));
        if (_subjectCodes.Contains(subjectCode))
            throw new InvalidOperationException($"Student	is	already	enrolled	in	{subjectCode}.");
        if (_subjectCodes.Count >= 7)
            throw new InvalidOperationException("A	student	may	not	be	enrolled	in	more	than	7	subjects.");
        _subjectCodes.Add(subjectCode);
    }
    //	NEW	—	an	update	method	for	Phase	8's	PUT	endpoint.
    //	Notice:	same	discipline	as	the	constructor.	Updating	is	not
    //	just	"set	the	property"	—	it	goes	through	validation	too.
    public void UpdateFullName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Full	name	is	required.", nameof(fullName));
        FullName = fullName;
    }
}

