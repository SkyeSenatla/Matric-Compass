namespace API.Data; 
using API.Models; 

//	The	contract.	Controllers	depend	on	THIS,	never	on	a	concrete
//	implementation.	Every	method	is	async	—	see	Phase	4's	rule.
public	interface	IStudentRepository
{
Task<IEnumerable<Student>>	GetAllAsync();
Task<Student?>	GetByIdAsync(Guid	id);
Task<Student?>	GetByLrnAsync(string	learnerReferenceNumber);
Task<Student>	AddAsync(Student	student);
Task<bool>	UpdateAsync(Student	student);
Task<bool>	DeleteAsync(Guid	id);
}