# Source - https://tex.stackexchange.com/a/40759
# Posted by DevSolar, modified by community. See post 'Timeline' for change history
# Retrieved 2026-06-07, License - CC BY-SA 4.0

# You want latexmk to *always* run, because make does not have all the info.
# Also, include non-file targets in .PHONY so they are run regardless of any
# file of the given name existing.
.PHONY: main.pdf all clean

# The first rule in a Makefile is the one executed by default ("make"). It
# should always be the "all" rule, so that "make" and "make all" are identical.
all: main.pdf


# MAIN LATEXMK RULE

# -pdf tells latexmk to generate PDF directly (instead of DVI).
# -pdflatex="" tells latexmk to call a specific backend with specific options.
# -use-make tells latexmk to call make for generating missing files.

# -interaction=nonstopmode keeps the pdflatex backend from stopping at a
# missing file reference and interactively asking you for an alternative.

main.pdf: main.tex
	latexmk -pdf -pdflatex="pdflatex -interaction=nonstopmode" -use-make main.tex

clean:
	rm -f main.aux main.bbl main.bcf main.blg \
	      main.fdb_latexmk main.fls main.log \
	      main.nav main.out main.run.xml \
	      main.snm main.toc main.synctex.gz \
		  main.bbl-SAVE-ERROR main.bcf-SAVE-ERROR
