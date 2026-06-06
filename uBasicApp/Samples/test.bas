1 rem This is a test program for uBasic
5 print "start of test"
10 gosub 100
20 for i = 1 to 127
30 print i
40 next i
50 print "end"
60 end
100 print "subroutine"
105 gosub 120
110 return
120 print "sub-subroutine"
130 return