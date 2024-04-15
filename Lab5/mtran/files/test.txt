import random

array1 = []
for _ in range(3):
    row = []
    for _ in range(3):
        row.append(random.randint(1, 10))
    array1.append(row)

array2 = []
for _ in range(3):
    row = []
    for _ in range(3):
        row.append(random.randint(1, 10))
    array2.append(row)

print("First:")
for row in array1:
    print(row)

print("Second:")
for row in array2:
    print(row)

sum_diag_array1 = 0
for i in range(3):
    sum_diag_array1 += array1[i][i]

sum_diag_array2 = 0
for i in range(3):
    sum_diag_array2 += array2[i][i]

print("Sum_First:")
print(sum_diag_array1)

print("Sum_Second:")
print(sum_diag_array2)

result = []

if sum_diag_array1 > sum_diag_array2:
    for i in range(3):
        row = []
        for j in range(3):
            row.append(array1[i][j] * array2[i][j])
        result.append(row)
    print("First multiply Second:")
elif sum_diag_array2 > sum_diag_array1:
    result = []
    for i in range(3):
        row = []
        for j in range(3):
            row.append(array2[i][j] / array1[i][j])
        result.append(row)
    print("Second devide First:")
else:
    print("Sum equal")

for row in result:
    print(row)
