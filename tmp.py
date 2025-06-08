for k in range(4, 21):
  # i = 1
  sum = 0

  # while True:
  for i in range(1, 11):
    cnt = k ** i
    sum += cnt
    # if sum >= 1e7:
    #   break
    print(sum, end = ", ")
  print()