Feature: Summa question reading
  As a visitor reading a question of the Summa Theologiae
  I want articles to open from the table of contents, from deep links, and to toggle closed
  So that I can navigate a question's articles and share links straight to them

  Scenario: Opening an article from the table of contents
	Given I open question "prima-q001"
	When I open article 2 from the table of contents
	Then article 2 should be expanded
	And the address bar should contain "#article-2"

  Scenario: Deep linking to an article opens it on arrival
	Given I open question "prima-q001" at article 2
	Then article 2 should be expanded

  Scenario: Toggling an open article closes it
	Given I open question "prima-q001" at article 2
	When I toggle article 2
	Then article 2 should be collapsed

  Scenario: A shorthand question id redirects to its canonical form
	Given I open question "prima-q1"
	Then the address bar should end with "summa/prima-q001"
