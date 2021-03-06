#!/usr/bin/perl
#
# Copyright (c) 2020 The Go Authors. All rights reserved.
#
# Use of this source code is governed by a BSD-style
# license that can be found in the LICENSE file.
#

# Modified version of make_perl_groups_cs.pl from RE2/CS:
# code.google.com/p/go/source/browse/src/pkg/regexp/syntax/make_perl_groups.pl
# which is in turn a modified version of RE2/C++ implementation.

# Generate table entries giving character ranges
# for POSIX/Perl character classes.  Rather than
# figure out what the definition is, it is easier to ask
# Perl about each letter from 0-128 and write down
# its answer.

@posixclasses = (
	"[:alnum:]",
	"[:alpha:]",
	"[:ascii:]",
	"[:blank:]",
	"[:cntrl:]",
	"[:digit:]",
	"[:graph:]",
	"[:lower:]",
	"[:print:]",
	"[:punct:]",
	"[:space:]",
	"[:upper:]",
	"[:word:]",
	"[:xdigit:]",
);

@perlclasses = (
	"\\d",
	"\\s",
	"\\w",
);

sub ComputeClass($) {
  my @ranges;
  my ($class) = @_;
  my $regexp = "[$class]";
  my $start = -1;
  for (my $i=0; $i<=129; $i++) {
    if ($i == 129) { $i = 256; }
    if ($i <= 128 && chr($i) =~ $regexp) {
      if ($start < 0) {
        $start = $i;
      }
    } else {
      if ($start >= 0) {
        push @ranges, [$start, $i-1];
      }
      $start = -1;
    }
  }
  return @ranges;
}

sub PrintClass($$@) {
  my ($cname, $groupmap, $name, @ranges) = @_;
  print "  private static readonly int[] code$cname = {  /* $name */\n";
  for (my $i=0; $i<@ranges; $i++) {
    my @a = @{$ranges[$i]};
    printf "\t0x%x, 0x%x,\n", $a[0], $a[1];
  }
  print "  };\n\n";
  my $n = @ranges;
  $negname = $name;
  if ($negname =~ /:/) {
    $negname =~ s/:/:^/;
  } else {
    $negname =~ y/a-z/A-Z/;
  }
  $name =~ s/\\/\\\\/g;
  $negname =~ s/\\/\\\\/g;
  return "    $groupmap.put(\"$name\",  \tnew CharGroup(+1, code$cname));\n" .
  	 "    $groupmap.put(\"$negname\",  \tnew CharGroup(-1, code$cname));\n";
}

my $gen = 0;

sub PrintClasses($@) {
  my ($cname, @classes) = @_;
  my $groupmap = uc($cname) . "_GROUPS";
  my @entries;
  foreach my $cl (@classes) {
    my @ranges = ComputeClass($cl);
    push @entries, PrintClass(++$gen, $groupmap, $cl, @ranges);
  }
  print "  static readonly Dictionary<String, CharGroup> $groupmap =\n";
  print "    new ();\n";
  print "\n";
  print "  static CharGroup() {\n";
  foreach my $e (@entries) {
    print $e;
  }
  print "  }\n";
  my $count = @entries;
}

print <<EOF;
// GENERATED BY make_perl_groups.pl; DO NOT EDIT.
// make_perl_groups_cs.pl >perl_groups.cs


public class CharGroup {

  int sign;
  int[] cls;

  private CharGroup(int sign, int[] cls) {
    this.sign = sign;
    this.cls = cls;
  }

EOF

PrintClasses("perl", @perlclasses);
PrintClasses("posix", @posixclasses);


print <<EOF;

}
EOF
